namespace Station.Logging;

public class LogContext
{
    private readonly string _formattedMessage;
    private readonly IPublicLogStream _stream;
    private readonly string _source;
    private readonly string _level;

    internal LogContext(
        string formattedMessage,
        IPublicLogStream stream,
        string source,
        string level)
    {
        _formattedMessage = formattedMessage;
        _stream = stream;
        _source = source;
        _level = level;
    }

    public void Public(string? correlationId = null)
    {
        _stream.Publish(new LogEntry
        {
            Source = _source,
            Level = _level,
            Message = _formattedMessage,
            CorrelationId = correlationId
        });
    }
}