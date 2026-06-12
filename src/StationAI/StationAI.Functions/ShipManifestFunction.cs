using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using StationAI.Core.Models;
using StationAI.Core.Services;
using System.Text.Json;

public class ShipManifestFunction
{
    private readonly ILogger<ShipManifestFunction> _logger;
    private readonly RiskAssessmentService _riskAssessmentService;

    public ShipManifestFunction(ILogger<ShipManifestFunction> logger, RiskAssessmentService riskAssessment)
    {
        _logger = logger;
        _riskAssessmentService = riskAssessment;

    }

    [Function("ShipManifestQueue")]
    public async Task Run(
        [QueueTrigger("ship-manifest-queue", Connection = "AzureWebJobsStorage1")] string message)
    {
        try
        {
            _logger.LogInformation("Received manifest message");

            var manifest = JsonSerializer.Deserialize<ShipManifest>(message);
            if (manifest is null)
            {
                _logger.LogError("Failed to deserialize manifest message");
                return;
            }

            var assessment = await _riskAssessmentService.AssessRisk(manifest);
            _logger.LogInformation("Assessment complete for {Callsign}: Bio={Bio} Chem={Chem} Sec={Sec} Recommendation:\"{Recommendation}\"",
                manifest.Callsign,
                assessment.BiohazardLevel,
                assessment.ChemicalHazardLevel,
                assessment.SecurityHazardLevel,
                assessment.Recommendation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing manifest message");
            throw;
        }
    }
}
