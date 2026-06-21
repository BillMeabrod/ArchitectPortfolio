namespace StationShipManifestLogger.Common.Data
{
    public class ManifestAuditLog
    {
        public int Id { get; set; }
        public required string Callsign { get; set; }
        public required string ShipName { get; set; }
        public required string CaptainName { get; set; }
        public DateTimeOffset LoggedAt { get; set; } = DateTimeOffset.UtcNow;
        public required string RawPayload { get; set; }
    }
}