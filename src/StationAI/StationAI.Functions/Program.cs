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
        services.AddScoped<RiskAssessmentService>();
        services.AddScoped<ILargeLanguageModelService, GeminiAdapter>();
        services.AddScoped<IRulesRepository, RulesBlobStorageAdapter>();
        services.AddSingleton(new BlobServiceClient(
            Environment.GetEnvironmentVariable("BlobStorageConnection")));
        services.AddSingleton(new QueueServiceClient(
            Environment.GetEnvironmentVariable("AzureWebJobsStorage")));
        services.AddScoped<RiskAssessmentQueuePublisher>();
    })    
    .Build();

await host.RunAsync();
