namespace Station.Logging;

public interface IStationLogger<T>
{
    LogContext Info(string template, params object?[] args);
    LogContext Warn(string template, params object?[] args);
    LogContext Error(string template, params object?[] args);
    LogContext Error(Exception ex, string template, params object?[] args);
}