using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Station.Logging;

public class StationLogger<T> : IStationLogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly IPublicLogStream _stream;
    private readonly string _source;

    // Matches ILogger-style named placeholders e.g. {Callsign}, {BiohazardLevel}
    // Negative lookbehind/lookahead ensures double-brace escapes {{ }} are not matched
    private static readonly Regex NamedPlaceholder = new(@"(?<!\{)\{(?!\{)[^}]+\}(?!\})", RegexOptions.Compiled);

    public StationLogger(ILogger<T> logger, IPublicLogStream stream, StationLoggerSource source)
    {
        _logger = logger;
        _stream = stream;
        _source = source.Value;
    }

    public void Info(string template, params object?[] args) =>
        _logger.LogInformation(template, args);

    public void InfoPublic(string template, string? correlationId, params object?[] args)
    {
        _logger.LogInformation(template, args);
        Publish(Format(template, args), correlationId, "INFO");
    }

    public void Warn(string template, params object?[] args) =>
        _logger.LogWarning(template, args);

    public void WarnPublic(string template, string? correlationId, params object?[] args)
    {
        _logger.LogWarning(template, args);
        Publish(Format(template, args), correlationId, "WARN");
    }

    public void Error(string template, params object?[] args) =>
        _logger.LogError(template, args);

    public void Error(Exception ex, string template, params object?[] args) =>
        _logger.LogError(ex, template, args);

    public void ErrorPublic(string template, string? correlationId, params object?[] args)
    {
        _logger.LogError(template, args);
        Publish(Format(template, args), correlationId, "ERROR");
    }

    public void ErrorPublic(Exception ex, string template, string? correlationId, params object?[] args)
    {
        _logger.LogError(ex, template, args);
        Publish(Format(template, args), correlationId, "ERROR");
    }

    private void Publish(string message, string? correlationId, string level) =>
        _stream.Publish(new LogEntry
        {
            Source = _source,
            Level = level,
            Message = message,
            CorrelationId = correlationId
        });

    // ILogger expects named placeholders ({Callsign}) which it stores as structured properties
    // in Azure Monitor, enabling field-level querying. string.Format requires positional
    // placeholders ({0}, {1}). We convert here so the public stream gets a fully interpolated
    // string while ILogger retains its structured format above.
    // Double braces ({{...}}) are ILogger's escape syntax for literal braces. The regex skips
    // them via negative lookaround, so string.Format renders them correctly as { and }.
    private static string Format(string template, object?[] args)
    {
        if (args.Length == 0)
            return template;

        int index = 0;
        var positional = NamedPlaceholder.Replace(template, _ => $"{{{index++}}}");

        try
        { return string.Format(positional, args); }
        catch { return template; }
    }
}