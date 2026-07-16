using Microsoft.Azure.Functions.Worker;
using Station.Logging;
using StationAI.Core.Models;
using StationAI.Core.Services;
using StationAI.Functions;
using System.Text.Json;

public class ShipManifestFunction
{
    private readonly RiskAssessmentService _riskAssessmentService;
    private readonly RiskAssessmentQueuePublisher _queuePublisher;
    private readonly IStationLogger<ShipManifestFunction> _log;

    public ShipManifestFunction(
        RiskAssessmentService riskAssessment,
        RiskAssessmentQueuePublisher queuePublisher,
        IStationLogger<ShipManifestFunction> log)
    {
        _riskAssessmentService = riskAssessment;
        _queuePublisher = queuePublisher;
        _log = log;
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
                _log.Error("Failed to deserialize manifest message");
                return;
            }

            _log.InfoPublic(
                "Manifest received for assessment — {ShipName} ({Callsign})",
                manifest.CorrelationId,
                manifest.ShipName, manifest.Callsign);

            var assessment = await _riskAssessmentService.AssessRisk(manifest);

            await _queuePublisher.PublishAsync(assessment, manifest);

            _log.InfoPublic(
                "Assessment dispatched to triage queue — {Callsign}",
                manifest.CorrelationId,
                manifest.Callsign);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error processing manifest message");
            throw;
        }
    }
}