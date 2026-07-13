using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Station.Logging;

public class PublicLogStream : IPublicLogStream
{
    private const int Capacity = 100;
    private readonly ConcurrentQueue<LogEntry> _history = new();
    private readonly List<Action<LogEntry>> _subscribers = new();
    private readonly Lock _lock = new();
    private readonly PublicLogPersistence _persistence;
    private readonly Channel<bool> _saveSignal = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public PublicLogStream(PublicLogPersistence persistence)
    {
        _persistence = persistence;
        _ = Task.Run(SaveLoopAsync);
    }

    public void Publish(LogEntry entry)
    {
        AddToHistory(entry);

        List<Action<LogEntry>> snapshot;
        lock (_lock)
            snapshot = new List<Action<LogEntry>>(_subscribers);

        foreach (var subscriber in snapshot)
        {
            try
            { subscriber(entry); }
            catch { }
        }

        _saveSignal.Writer.TryWrite(true);
    }

    public IReadOnlyList<LogEntry> GetHistory() => _history.ToArray();

    public void SeedHistory(IEnumerable<LogEntry> history)
    {
        foreach (var entry in history)
            AddToHistory(entry);
    }

    public IDisposable Subscribe(Action<LogEntry> handler)
    {
        lock (_lock)
            _subscribers.Add(handler);

        return new Subscription(() =>
        {
            lock (_lock)
                _subscribers.Remove(handler);
        });
    }

    private void AddToHistory(LogEntry entry)
    {
        _history.Enqueue(entry);
        while (_history.Count > Capacity)
            _history.TryDequeue(out _);
    }

    private async Task SaveLoopAsync()
    {
        await foreach (var _ in _saveSignal.Reader.ReadAllAsync())
        {
            try
            { await _persistence.SaveAsync(GetHistory()); }
            catch { }
        }
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}