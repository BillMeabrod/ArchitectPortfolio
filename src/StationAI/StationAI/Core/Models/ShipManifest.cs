namespace StationAI.Core.Models
{
    public class ShipManifest
    {
        public string ShipName { get; set; } = string.Empty;
        public string Callsign { get; set; } = string.Empty;
        public string CaptainName { get; set; } = string.Empty;
        public List<string> CargoItems { get; set; } = [];
        public List<string> Passengers { get; set; } = [];
        public string CorrelationId { get; set; } = string.Empty;
    }
}