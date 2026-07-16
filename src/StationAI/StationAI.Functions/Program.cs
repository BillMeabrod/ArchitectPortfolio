using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Station.Logging;
using StationAI.Adapters.Outbound;
using StationAI.Core.Interfaces;
using StationAI.Core.Services;
using StationAI.Functions;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var blobStorageConnection = Environment.GetEnvironmentVariable("BlobStorageConnection")
            ?? throw new InvalidOperationException("BlobStorageConnection environment variable is not set. Fix your configuration.");
        var azureWebJobsStorage = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException("AzureWebJobsStorage environment variable is not set. Fix your configuration.");

        var qdrantUrl = Environment.GetEnvironmentVariable("Qdrant__Url")
            ?? throw new InvalidOperationException("Qdrant:Url is not set. Fix your configuration.");
        var qdrantApiKey = Environment.GetEnvironmentVariable("Qdrant__ApiKey")
            ?? throw new InvalidOperationException("Qdrant:ApiKey is not set. Fix your configuration.");
        var qdrantCollection = Environment.GetEnvironmentVariable("Qdrant__Collection") ?? "station-lore";

        var blobServiceClient = new BlobServiceClient(blobStorageConnection);
        services.AddSingleton(blobServiceClient);

        services.AddStationLogging("station-ai", "ARIA");

        services.AddScoped<RiskAssessmentService>();
        services.AddScoped<ILargeLanguageModelService, GeminiAdapter>();
        services.AddScoped<IStationDirectiveRepository, RulesBlobStorageAdapter>();
        services.AddScoped<IDirectiveTargetRepository, DirectiveTargetBlobStorageAdapter>();
        services.AddSingleton<IEmbeddingService, GoogleEmbeddingAdapter>();
        services.AddSingleton<ILoreRepository>(sp =>
            new QdrantLoreAdapter(qdrantUrl, qdrantApiKey, qdrantCollection,
                sp.GetRequiredService<IEmbeddingService>()));
        services.AddSingleton(new QueueServiceClient(azureWebJobsStorage));
        services.AddScoped<RiskAssessmentQueuePublisher>();
    })
    .Build();

await host.Services.WarmPublicLogStreamAsync();

await host.RunAsync();
