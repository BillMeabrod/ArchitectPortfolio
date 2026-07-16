using Microsoft.AspNetCore.Mvc;
using Station.Logging;
using System.Text;
using System.Text.Json;

namespace StationShipManifestLogger.Features.LogStream
{
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

            foreach (var entry in _stream.GetHistory())
                await WriteEntry(entry, cancellationToken);

            var tcs = new TaskCompletionSource();

            using var subscription = _stream.Subscribe(async entry =>
            {
                try
                { await WriteEntry(entry, cancellationToken); }
                catch { tcs.TrySetResult(); }
            });

            using var reg = cancellationToken.Register(() => tcs.TrySetResult());
            await tcs.Task;
        }

        private async Task WriteEntry(LogEntry entry, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(entry);
            await Response.WriteAsync($"data: {json}\n\n", Encoding.UTF8, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }
}