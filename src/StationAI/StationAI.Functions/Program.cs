using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StationAI.Adapters.Outbound;
using StationAI.Core.Interfaces;
using StationAI.Core.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddScoped<RiskAssessmentService>();
        services.AddScoped<ILargeLanguageModelService, GeminiAdapter>();
        services.AddScoped<IRulesRepository, RulesBlobStorageAdapter>();
        services.AddSingleton(new BlobServiceClient(
            Environment.GetEnvironmentVariable("BlobStorageConnection")));
    })
    .Build();

await host.RunAsync();
