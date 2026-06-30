using Azure.Storage.Blobs;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using StationAI.Adapters.Outbound;
using StationAI.Core.Interfaces;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins("https://agreeable-moss-0ff2e0510.7.azurestaticapps.net")
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("UniverseRulesSave", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

var blobStorageConnection = builder.Configuration.GetConnectionString("BlobStorageConnection")
    ?? throw new InvalidOperationException("BlobStorageConnection connection string is not set. Fix your configuration.");

builder.Services.AddSingleton(new BlobServiceClient(blobStorageConnection));
builder.Services.AddScoped<IRulesRepository, RulesBlobStorageAdapter>();
builder.Services.AddScoped<ILargeLanguageModelService, GeminiAdapter>();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("DashboardPolicy");
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();
app.UseHealthChecks("/health");
app.Run();
