using Azure.Storage.Queues;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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
    }

    public class SubmitManifestReportHandler(ManifestLoggerDbContext context, ShipManifestQueuePublisher queuePublisher) : IRequestHandler<ManifestReportCommand, int>
    {
        private readonly ManifestLoggerDbContext _context = context;
        private readonly ShipManifestQueuePublisher _queuePublisher = queuePublisher;

        public async Task<int> Handle(ManifestReportCommand request, CancellationToken cancellationToken)
        {
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

            await _queuePublisher.PublishAsync(request);

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

        public async Task PublishAsync(ShipManifestQueuePublisher manifest)
        {
            await _queueClient.CreateIfNotExistsAsync();
            var json = JsonSerializer.Serialize(manifest);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            await _queueClient.SendMessageAsync(base64);
        }
    }
}