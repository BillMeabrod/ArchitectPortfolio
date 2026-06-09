using MediatR;
using Microsoft.AspNetCore.Mvc;
using StationShipManifestLogger.Common.Data;
using System.ComponentModel.DataAnnotations;
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
            return Ok($"Successfully audited manifest under Audit ID {auditId}");
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

    public class SubmitManifestReportHandler(StationDbContext context) : IRequestHandler<ManifestReportCommand, int>
    {
        private readonly StationDbContext _context = context;

        public async Task<int> Handle(ManifestReportCommand request, CancellationToken cancellationToken)
        {
            var logEntry = new ManifestAuditLog
            {
                Callsign = request.Callsign,
                CaptainName = request.CaptainName,
                ShipName = request.ShipName,
                RawPayload = JsonSerializer.Serialize(request),
                LoggedAt = DateTime.UtcNow
            };

            _context.ManifestAuditLogs.Add(logEntry);
            await _context.SaveChangesAsync(cancellationToken);
            return logEntry.Id;
        }
    }
}
