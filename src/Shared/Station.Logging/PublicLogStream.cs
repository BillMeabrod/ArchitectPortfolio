using System.Collections.Concurrent;

namespace Station.Logging;

public class PublicLogStream : IPublicLogStream
{
    private const int Capacity = 100;
    private readonly ConcurrentQueue<LogEntry> _history = new();
    private readonly List<Action<LogEntry>> _subscribers = new();
    private readonly Lock _lock = new();
    private readonly Lock _saveLock = new();
    private readonly PublicLogPersistence _persistence;
    private Task _saveTask = Task.CompletedTask;
    private bool _savePending;

    public PublicLogStream(PublicLogPersistence persistence)
    {
        _persistence = persistence;
    }

    public void Publish(LogEntry entry)
    {
        AddToHistory(entry);

        List<Action<LogEntry>> subscribers;
        lock (_lock)
            subscribers = new List<Action<LogEntry>>(_subscribers);
        foreach (var subscriber in subscribers)
        {
            try
            {
                subscriber(entry);
            }
            catch
            {
                // Intentionally ignore subscriber failures to avoid breaking the log pipeline.
            }
        }

        ScheduleSave();
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

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }

    private void AddToHistory(LogEntry entry)
    {
        _history.Enqueue(entry);

        var count = _history.Count;
        while (count > Capacity && _history.TryDequeue(out _))
            count--;
    }

    private void ScheduleSave()
    {
        lock (_saveLock)
        {
            if (_saveTask.IsCompleted)
            {
                _saveTask = PersistHistoryAsync();
                return;
            }

            _savePending = true;
        }
    }

    private async Task PersistHistoryAsync()
    {
        while (true)
        {
            await _persistence.SaveAsync(GetHistory());

            lock (_saveLock)
            {
                if (!_savePending)
                {
                    _saveTask = Task.CompletedTask;
                    return;
                }

                _savePending = false;
            }
        }
    }
}