namespace Station.Logging;

public interface IPublicLogStream
{
    void Publish(LogEntry entry);
    IReadOnlyList<LogEntry> GetHistory();
    IDisposable Subscribe(Action<LogEntry> handler);
}