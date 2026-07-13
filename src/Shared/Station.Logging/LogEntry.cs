namespace Station.Logging
{
    public class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string Source { get; set; } = string.Empty;
        public string Level { get; set; } = "INFO";
        public string? CorrelationId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
