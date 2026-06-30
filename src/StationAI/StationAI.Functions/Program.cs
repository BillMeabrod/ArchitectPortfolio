using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        services.AddScoped<RiskAssessmentService>();
        services.AddScoped<ILargeLanguageModelService, GeminiAdapter>();
        services.AddScoped<IRulesRepository, RulesBlobStorageAdapter>();
        services.AddSingleton(new BlobServiceClient(blobStorageConnection));
        services.AddSingleton(new QueueServiceClient(azureWebJobsStorage));
        services.AddScoped<RiskAssessmentQueuePublisher>();
    })
    .Build();

await host.RunAsync();
