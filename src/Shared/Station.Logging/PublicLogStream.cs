using System.Collections.Concurrent;

namespace Station.Logging;

public class PublicLogStream : IPublicLogStream
{
    private const int Capacity = 100;
    private readonly ConcurrentQueue<LogEntry> _history = new();
    private readonly List<Action<LogEntry>> _subscribers = new();
    private readonly Lock _lock = new();
    private readonly PublicLogPersistence _persistence;

    public PublicLogStream(PublicLogPersistence persistence)
    {
        _persistence = persistence;
    }

    public void Publish(LogEntry entry)
    {
        _history.Enqueue(entry);
        while (_history.Count > Capacity)
            _history.TryDequeue(out _);

        lock (_lock)
            foreach (var subscriber in _subscribers)
                subscriber(entry);

        _ = _persistence.SaveAsync(GetHistory());
    }

    public IReadOnlyList<LogEntry> GetHistory() => _history.ToArray();

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

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}