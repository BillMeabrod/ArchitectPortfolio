using System.Text;
using System.Text.Json;

namespace Station.Logging;

/// <summary>
/// Forwards public log entries to a remote API's ingest endpoint.
/// Use this in processes that don't own the public stream (e.g. a Functions host
/// that forwards to a co-located API). The API registers the real PublicLogStream;
/// this adapter is a lightweight producer over HTTP.
/// </summary>
public class HttpPublicLogStream : IPublicLogStream
{
    private readonly HttpClient _http;

    public HttpPublicLogStream(string apiBaseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/") };
    }

    public void Publish(LogEntry entry)
    {
        // Fire and forget — log forwarding must never block the caller
        _ = Task.Run(async () =>
        {
            try
            {
                var json = JsonSerializer.Serialize(entry);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                await _http.PostAsync("api/logs/ingest", content);
            }
            catch { }
        });
    }

    // Not meaningful in a forwarding context — the remote API owns history and subscriptions
    public IReadOnlyList<LogEntry> GetHistory() => [];
    public IDisposable Subscribe(Action<LogEntry> handler) => new NoOpDisposable();

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}