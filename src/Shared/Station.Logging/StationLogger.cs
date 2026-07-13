using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Station.Logging;

public class StationLogger<T> : IStationLogger<T>
{
    private readonly ILogger<T> _logger;
    private readonly IPublicLogStream _stream;
    private readonly string _source;

    // Matches ILogger-style named placeholders e.g. {Callsign}, {BiohazardLevel}
    private static readonly Regex NamedPlaceholder = new(@"\{[^}]+\}", RegexOptions.Compiled);

    public StationLogger(ILogger<T> logger, IPublicLogStream stream, StationLoggerSource source)
    {
        _logger = logger;
        _stream = stream;
        _source = source.Value;
    }

    public LogContext Info(string template, params object?[] args)
    {
        _logger.LogInformation(template, args);
        return new LogContext(Format(template, args), _stream, _source, "INFO");
    }

    public LogContext Warn(string template, params object?[] args)
    {
        _logger.LogWarning(template, args);
        return new LogContext(Format(template, args), _stream, _source, "WARN");
    }

    public LogContext Error(string template, params object?[] args)
    {
        _logger.LogError(template, args);
        return new LogContext(Format(template, args), _stream, _source, "ERROR");
    }

    public LogContext Error(Exception ex, string template, params object?[] args)
    {
        _logger.LogError(ex, template, args);
        return new LogContext(Format(template, args), _stream, _source, "ERROR");
    }

    // ILogger expects named placeholders ({Callsign}) which it stores as structured properties
    // in Azure Monitor, enabling field-level querying. string.Format requires positional
    // placeholders ({0}, {1}). We convert here so the public stream gets a fully interpolated
    // string while ILogger retains its structured format above.
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