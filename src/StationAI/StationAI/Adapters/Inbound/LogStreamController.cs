using Microsoft.AspNetCore.Mvc;
using Station.Logging;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace StationAI.Adapters.Inbound;

[ApiController]
[Route("api/logs")]
public class LogStreamController : ControllerBase
{
    private readonly PublicLogStream _stream;

    public LogStreamController(PublicLogStream stream)
    {
        _stream = stream;
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var channel = Channel.CreateUnbounded<LogEntry>(
            new UnboundedChannelOptions { SingleReader = true });

        using var subscription = _stream.Subscribe(entry =>
            channel.Writer.TryWrite(entry));

        foreach (var entry in _stream.GetHistory())
            await WriteEntry(entry, cancellationToken);

        await foreach (var entry in channel.Reader.ReadAllAsync(cancellationToken))
        {
            try
            { await WriteEntry(entry, cancellationToken); }
            catch { break; }
        }
    }

    [HttpPost("ingest")]
    public IActionResult Ingest([FromBody] LogEntry entry)
    {
        _stream.Publish(entry);
        return Ok();
    }

    private async Task WriteEntry(LogEntry entry, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(entry);
        await Response.WriteAsync($"data: {json}\n\n", Encoding.UTF8, cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}