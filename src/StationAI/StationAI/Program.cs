using Azure.Storage.Blobs;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using StationAI.Adapters.Outbound;
using StationAI.Core.Interfaces;
using StationAI.Core.Services;
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
    options.AddFixedWindowLimiter("StationDirectiveSave", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddSingleton<IEmbeddingService, GoogleEmbeddingAdapter>();

builder.Services.AddSingleton<ILoreRepository>(sp =>
{
    var dbUrl = builder.Configuration.GetConnectionString("DatabaseUrl")
        ?? throw new InvalidOperationException("DatabaseUrl connection string is required");
    var qdrantUrl = builder.Configuration["Qdrant:Url"]
        ?? throw new InvalidOperationException("Qdrant:Url configuration is required");
    var qdrantApiKey = builder.Configuration["Qdrant:ApiKey"]
        ?? throw new InvalidOperationException("Qdrant:ApiKey configuration is required");
    var embeddingService = sp.GetRequiredService<IEmbeddingService>();

    var collectionName = builder.Configuration["Qdrant:Collection"] ?? "station-lore";

    return new QdrantLoreAdapter(dbUrl, qdrantUrl, qdrantApiKey, collectionName, embeddingService);
});

var blobStorageConnection = builder.Configuration.GetConnectionString("BlobStorageConnection")
    ?? throw new InvalidOperationException("BlobStorageConnection connection string is not set. Fix your configuration.");

builder.Services.AddSingleton(new BlobServiceClient(blobStorageConnection));
builder.Services.AddScoped<IStationDirectiveRepository, RulesBlobStorageAdapter>();
builder.Services.AddScoped<ILargeLanguageModelService, GeminiAdapter>();
builder.Services.AddScoped<IDirectiveParsingService, DirectiveParsingService>();
builder.Services.AddScoped<IDirectiveTargetRepository, DirectiveTargetBlobStorageAdapter>();
builder.Services.AddScoped<IStationDirectiveService, StationDirectiveService>();
builder.Services.AddScoped<IModerationService, ModerationService>();
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
