using Azure.Storage.Blobs;
using StationAI.Adapters.Outbound;
using StationAI.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy", policy =>
    {
        policy.WithOrigins(
        "http://localhost:5173",
        "https://agreeable-moss-0ff2e0510.7.azurestaticapps.net"
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

builder.Services.AddSingleton(new BlobServiceClient(
    builder.Configuration.GetConnectionString("BlobStorageConnection")));
builder.Services.AddScoped<IRulesRepository, RulesBlobStorageAdapter>();
builder.Services.AddScoped<ILargeLanguageModelService, GeminiAdapter>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("DashboardPolicy");

app.UseAuthorization();

app.MapControllers();
app.UseHealthChecks("/health");
app.Run();
