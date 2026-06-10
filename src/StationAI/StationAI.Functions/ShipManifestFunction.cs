using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

public class ShipManifestFunction
{
    private readonly ILogger<ShipManifestFunction> _logger;

    public ShipManifestFunction(ILogger<ShipManifestFunction> logger)
    {
        _logger = logger;
    }

    [Function("ShipManifestQueue")]
    public async Task Run(
        [QueueTrigger("ship-manifest-queue", Connection = "AzureWebJobsStorage1")] string message)
    {
        _logger.LogInformation("Received manifest: {Message}", message);
    }
}
