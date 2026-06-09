namespace StationShipManifestLogger.Common.Data
{
    public class ManifestAuditLog
    {
        public int Id { get; set; }
        public string Callsign { get; set; }
        public string ShipName { get; set; }
        public string CaptainName { get; set; }
        public DateTimeOffset LoggedAt { get; set; } = DateTime.UtcNow;
        public string RawPayload { get; set; }
    }
}
