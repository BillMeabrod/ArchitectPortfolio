using Microsoft.Extensions.Logging;
using System.Text;
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

        try
        { return string.Format(ConvertNamedPlaceholders(template), args); }
        catch { return template; }
    }

    private static string ConvertNamedPlaceholders(string template)
    {
        if (!NamedPlaceholder.IsMatch(template))
            return template;

        Dictionary<string, int> placeholderIndexes = [];
        StringBuilder builder = new(template.Length);
        int nextIndex = 0;

        for (int i = 0; i < template.Length; i++)
        {
            if (template[i] == '{')
            {
                if (IsEscapedBrace(template, i))
                {
                    builder.Append("{{");
                    i++;
                    continue;
                }

                int end = template.IndexOf('}', i + 1);
                if (end < 0)
                    return template;

                var placeholder = template[(i + 1)..end];
                if (placeholder.Length == 0 || placeholder.IndexOf('{') >= 0)
                    return template;

                int suffixIndex = placeholder.IndexOfAny([',', ':']);
                var placeholderName = suffixIndex >= 0
                    ? placeholder[..suffixIndex]
                    : placeholder;

                if (!placeholderIndexes.TryGetValue(placeholderName, out var index))
                    placeholderIndexes[placeholderName] = index = nextIndex++;

                builder.Append('{').Append(index);
                if (suffixIndex >= 0)
                    builder.Append(placeholder.AsSpan(suffixIndex));
                builder.Append('}');
                i = end;
                continue;
            }

            if (template[i] == '}' && IsEscapedBrace(template, i))
            {
                builder.Append("}}");
                i++;
                continue;
            }

            builder.Append(template[i]);
        }

        return builder.ToString();
    }

    private static bool IsEscapedBrace(string template, int index) =>
        index + 1 < template.Length &&
        template[index + 1] == template[index];
}