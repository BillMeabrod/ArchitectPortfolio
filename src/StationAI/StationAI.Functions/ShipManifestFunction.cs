using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Station.Logging;
using StationAI.Core.Models;
using StationAI.Core.Services;
using StationAI.Functions;
using System.Text.Json;

public class ShipManifestFunction
{
    private readonly ILogger<ShipManifestFunction> _logger;
    private readonly RiskAssessmentService _riskAssessmentService;
    private readonly RiskAssessmentQueuePublisher _queuePublisher;
    private readonly IPublicLogStream _publicLog;

    public ShipManifestFunction(
        ILogger<ShipManifestFunction> logger,
        RiskAssessmentService riskAssessment,
        RiskAssessmentQueuePublisher queuePublisher,
        IPublicLogStream publicLog)
    {
        _logger = logger;
        _riskAssessmentService = riskAssessment;
        _queuePublisher = queuePublisher;
        _publicLog = publicLog;
    }

    [Function("ShipManifestQueue")]
    public async Task Run(
        [QueueTrigger("ship-manifest-queue", Connection = "AzureWebJobsStorage")] string message)
    {
        try
        {
            var manifest = JsonSerializer.Deserialize<ShipManifest>(message);
            if (manifest is null)
            {
                _logger.LogError("Failed to deserialize manifest message");
                return;
            }

            _publicLog.Publish(new LogEntry
            {
                Source = "ARIA",
                Level = "INFO",
                CorrelationId = manifest.CorrelationId,
                Message = $"Manifest received for assessment — {manifest.ShipName} ({manifest.Callsign})"
            });

            var assessment = await _riskAssessmentService.AssessRisk(manifest);

            _publicLog.Publish(new LogEntry
            {
                Source = "ARIA",
                Level = "INFO",
                CorrelationId = manifest.CorrelationId,
                Message = $"Dispatching assessment to triage queue — {manifest.Callsign}"
            });

            await _queuePublisher.PublishAsync(assessment, manifest);

            _logger.LogInformation("Assessment complete for {Callsign}: Bio={Bio} Chem={Chem} Sec={Sec}",
                manifest.Callsign,
                assessment.BiohazardLevel,
                assessment.ChemicalHazardLevel,
                assessment.SecurityHazardLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing manifest message");
            throw;
        }
    }
}