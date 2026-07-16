using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Station.Logging.Tests;

public class StationLoggingTests
{
    [Fact]
    public void PublicLogStream_KeepsOnlyMostRecent100Entries()
    {
        var stream = new PublicLogStream(new FakePublicLogPersistence());

        for (int i = 0; i < 105; i++)
        {
            stream.Publish(new LogEntry
            {
                Message = $"entry-{i}",
                Source = "StationAI"
            });
        }

        var history = stream.GetHistory();

        Assert.Equal(100, history.Count);
        Assert.Equal("entry-5", history[0].Message);
        Assert.Equal("entry-104", history[^1].Message);
    }

    [Fact]
    public async Task PublicLogStream_CoalescesPendingSavesDuringBurstLogging()
    {
        var persistence = new BlockingPublicLogPersistence();
        var stream = new PublicLogStream(persistence);

        stream.Publish(new LogEntry { Message = "entry-0", Source = "StationAI" });
        await persistence.FirstSaveStarted.WaitAsync(TimeSpan.FromSeconds(5));

        for (int i = 1; i < 5; i++)
        {
            stream.Publish(new LogEntry
            {
                Message = $"entry-{i}",
                Source = "StationAI"
            });
        }

        Assert.Equal(1, persistence.SaveCallCount);

        persistence.ReleaseFirstSave();

        await persistence.SecondSaveStarted.WaitAsync(TimeSpan.FromSeconds(5));
        await persistence.SecondSaveCompleted.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(2, persistence.SaveCallCount);
    }

    [Fact]
    public async Task WarmPublicLogStreamAsync_SeedsHistoryWithoutPublishSideEffects()
    {
        var persistence = new FakePublicLogPersistence
        {
            LoadedEntries =
            [
                new LogEntry { Message = "entry-0", Source = "StationAI" },
                new LogEntry { Message = "entry-1", Source = "StationAI" }
            ]
        };
        var stream = new PublicLogStream(persistence);
        int notifications = 0;

        using var _ = stream.Subscribe(_ => notifications++);
        var services = new DictionaryServiceProvider(new Dictionary<Type, object>
        {
            [typeof(PublicLogPersistence)] = persistence,
            [typeof(PublicLogStream)] = stream
        });

        await services.WarmPublicLogStreamAsync();

        var history = stream.GetHistory();
        Assert.Equal(2, history.Count);
        Assert.Equal("entry-0", history[0].Message);
        Assert.Equal("entry-1", history[1].Message);
        Assert.Equal(0, notifications);
        Assert.Equal(0, persistence.SaveCallCount);
    }

    [Fact]
    public void StationLogger_InfoPublic_FormatsNamedPlaceholdersAndEscapedBraces()
    {
        var stream = new RecordingPublicLogStream();
        var logger = new StationLogger<StationLoggingTests>(
            NullLogger<StationLoggingTests>.Instance,
            stream,
            new StationLoggerSource("StationAI"));

        logger.InfoPublic("Docking {{bay}} assigned to {Callsign}. Repeat {Callsign}.", "corr-123", "NR-007");

        var entry = Assert.Single(stream.Entries);
        Assert.Equal("Docking {bay} assigned to NR-007. Repeat NR-007.", entry.Message);
        Assert.Equal("INFO", entry.Level);
        Assert.Equal("StationAI", entry.Source);
        Assert.Equal("corr-123", entry.CorrelationId);
    }

    [Fact]
    public void HttpPublicLogStream_GetHistory_ReturnsEmpty()
    {
        var stream = new HttpPublicLogStream("https://example.com");

        Assert.Empty(stream.GetHistory());
    }

    [Fact]
    public void HttpPublicLogStream_Subscribe_ReturnsNoOpDisposable()
    {
        var stream = new HttpPublicLogStream("https://example.com");
        var callCount = 0;

        var subscription = stream.Subscribe(_ => callCount++);
        stream.Publish(new LogEntry { Message = "test", Source = "ARIA" });
        subscription.Dispose();

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void AddStationLoggingForwarder_RegistersHttpPublicLogStream()
    {
        var services = new ServiceCollection();
        services.AddStationLoggingForwarder("https://example.com", "ARIA");

        var provider = services.BuildServiceProvider();
        var stream = provider.GetRequiredService<IPublicLogStream>();

        Assert.IsType<HttpPublicLogStream>(stream);
    }

    [Fact]
    public void AddStationLoggingForwarder_ResolvesIStationLogger()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStationLoggingForwarder("https://example.com", "ARIA");

        var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<IStationLogger<StationLoggingTests>>();

        Assert.NotNull(logger);
    }

    private sealed class RecordingPublicLogStream : IPublicLogStream
    {
        public List<LogEntry> Entries { get; } = [];

        public void Publish(LogEntry entry) => Entries.Add(entry);
        public IReadOnlyList<LogEntry> GetHistory() => Entries;
        public IDisposable Subscribe(Action<LogEntry> handler) => new NoOpDisposable();
    }

    private sealed class FakePublicLogPersistence : PublicLogPersistence
    {
        public FakePublicLogPersistence()
            : base(new BlobServiceClient(new Uri("https://example.com")), "station-ai")
        {
        }

        public List<LogEntry> LoadedEntries { get; set; } = [];
        public int SaveCallCount { get; private set; }

        public override Task<List<LogEntry>> LoadAsync() => Task.FromResult(LoadedEntries);

        public override Task SaveAsync(IReadOnlyList<LogEntry> entries)
        {
            SaveCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class BlockingPublicLogPersistence : PublicLogPersistence
    {
        private readonly TaskCompletionSource _firstSaveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseFirstSave = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _secondSaveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _secondSaveCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public BlockingPublicLogPersistence()
            : base(new BlobServiceClient(new Uri("https://example.com")), "station-ai")
        {
        }

        public Task FirstSaveStarted => _firstSaveStarted.Task;
        public Task SecondSaveStarted => _secondSaveStarted.Task;
        public Task SecondSaveCompleted => _secondSaveCompleted.Task;
        public int SaveCallCount { get; private set; }

        public override async Task SaveAsync(IReadOnlyList<LogEntry> entries)
        {
            SaveCallCount++;

            if (SaveCallCount == 1)
            {
                _firstSaveStarted.TrySetResult();
                await _releaseFirstSave.Task;
                return;
            }

            if (SaveCallCount == 2)
            {
                _secondSaveStarted.TrySetResult();
                _secondSaveCompleted.TrySetResult();
            }
        }

        public void ReleaseFirstSave() => _releaseFirstSave.TrySetResult();
    }

    private sealed class DictionaryServiceProvider(Dictionary<Type, object> services) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            services.TryGetValue(serviceType, out var service)
                ? service
                : null;
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }
}