namespace Station.Logging;

public interface IStationLogger<T>
{
    void Info(string template, params object?[] args);
    void InfoPublic(string template, string? correlationId, params object?[] args);
    void Warn(string template, params object?[] args);
    void WarnPublic(string template, string? correlationId, params object?[] args);
    void Error(string template, params object?[] args);
    void Error(Exception ex, string template, params object?[] args);
    void ErrorPublic(string template, string? correlationId, params object?[] args);
    void ErrorPublic(Exception ex, string template, string? correlationId, params object?[] args);
}