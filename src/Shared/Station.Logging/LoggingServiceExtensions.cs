using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Station.Logging;

public static class LoggingServiceExtensions
{
    /// <summary>
    /// Registers the full in-memory public log stream with blob persistence and IStationLogger.
    /// Use this in the process that owns the stream and serves the SSE endpoint.
    /// Call WarmPublicLogStreamAsync after app.Build() to restore history from blob storage.
    /// </summary>
    public static IServiceCollection AddStationLogging(
        this IServiceCollection services,
        string appName,
        string source)
    {
        services.AddSingleton(sp =>
            new PublicLogPersistence(
                sp.GetRequiredService<BlobServiceClient>(),
                appName));

        services.AddSingleton<PublicLogStream>();
        services.AddSingleton<IPublicLogStream>(sp =>
            sp.GetRequiredService<PublicLogStream>());

        services.AddSingleton(new StationLoggerSource(source));
        services.AddTransient(typeof(IStationLogger<>), typeof(StationLogger<>));

        return services;
    }

    /// <summary>
    /// Registers an HTTP forwarder as IPublicLogStream and IStationLogger.
    /// Use this in processes that don't own the stream (e.g. a Functions host alongside an API).
    /// Log entries are forwarded to the target API's POST /api/logs/ingest endpoint.
    /// </summary>
    public static IServiceCollection AddStationLoggingForwarder(
        this IServiceCollection services,
        string apiBaseUrl,
        string source)
    {
        services.AddSingleton<IPublicLogStream>(new HttpPublicLogStream(apiBaseUrl));
        services.AddSingleton(new StationLoggerSource(source));
        services.AddTransient(typeof(IStationLogger<>), typeof(StationLogger<>));

        return services;
    }

    public static async Task WarmPublicLogStreamAsync(this IServiceProvider services)
    {
        var persistence = services.GetRequiredService<PublicLogPersistence>();
        var stream = services.GetRequiredService<PublicLogStream>();

        var history = await persistence.LoadAsync();
        stream.SeedHistory(history);
    }
}

public record StationLoggerSource(string Value);