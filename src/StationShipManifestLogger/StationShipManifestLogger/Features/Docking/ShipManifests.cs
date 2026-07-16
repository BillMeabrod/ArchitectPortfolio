using Azure.Storage.Queues;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Station.Logging;
using StationShipManifestLogger.Common.Data;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace StationShipManifestLogger.Features.Docking
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipManifestsController(IMediator mediatr) : ControllerBase
    {
        private readonly IMediator _mediatr = mediatr;

        [HttpPost]
        [Route("submit-manifest")]
        [EnableRateLimiting("ManifestSubmission")]
        public async Task<IActionResult> Handle([FromBody] ManifestReportCommand manifestReport)
        {
            int auditId = await _mediatr.Send(manifestReport);
            return Ok(new { auditId, message = $"Successfully audited manifest under Audit ID {auditId}" });
        }
    }

    public record ManifestReportCommand : IRequest<int>
    {
        [Required]
        public string ShipName { get; set; } = string.Empty;

        [Required]
        public string Callsign { get; set; } = string.Empty;

        [Required]
        public string CaptainName { get; set; } = string.Empty;

        public List<string> CargoItems { get; set; } = [];

        public List<string> Passengers { get; set; } = [];

        public string CorrelationId { get; set; } = string.Empty;
    }

    public class SubmitManifestReportHandler(
        ManifestLoggerDbContext context,
        ShipManifestQueuePublisher queuePublisher,
        IStationLogger<SubmitManifestReportHandler> log) : IRequestHandler<ManifestReportCommand, int>
    {
        private readonly ManifestLoggerDbContext _context = context;
        private readonly ShipManifestQueuePublisher _queuePublisher = queuePublisher;
        private readonly IStationLogger<SubmitManifestReportHandler> _log = log;

        public async Task<int> Handle(ManifestReportCommand request, CancellationToken cancellationToken)
        {
            request.CorrelationId = Guid.NewGuid().ToString("N")[..8];

            _log.Info("Manifest received — {ShipName} ({Callsign}), Captain: {CaptainName}, Cargo: {CargoCount} item(s), Passengers: {PassengerCount}",
                request.ShipName, request.Callsign, request.CaptainName,
                request.CargoItems.Count, request.Passengers.Count)
                .Public(request.CorrelationId);

            var logEntry = new ManifestAuditLog
            {
                Callsign = request.Callsign,
                CaptainName = request.CaptainName,
                ShipName = request.ShipName,
                RawPayload = JsonSerializer.Serialize(request),
                LoggedAt = DateTimeOffset.UtcNow
            };

            _context.ManifestAuditLogs.Add(logEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _log.Info("Manifest logged to audit — {Callsign}, Audit ID: {AuditId}",
                request.Callsign, logEntry.Id)
                .Public(request.CorrelationId);

            await _queuePublisher.PublishAsync(request);

            _log.Info("Manifest dispatched to assessment queue — {Callsign}", request.Callsign)
                .Public(request.CorrelationId);

            return logEntry.Id;
        }
    }

    public class ShipManifestQueuePublisher
    {
        private readonly QueueClient _queueClient;

        public ShipManifestQueuePublisher(QueueServiceClient queueServiceClient)
        {
            _queueClient = queueServiceClient.GetQueueClient("ship-manifest-queue");
        }

        public async Task PublishAsync(ManifestReportCommand manifest)
        {
            await _queueClient.CreateIfNotExistsAsync();
            var json = JsonSerializer.Serialize(manifest);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            await _queueClient.SendMessageAsync(base64);
        }
    }
}