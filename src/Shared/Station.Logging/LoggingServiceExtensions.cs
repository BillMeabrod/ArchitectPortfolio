using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;

namespace Station.Logging;

public static class LoggingServiceExtensions
{
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

    public static async Task WarmPublicLogStreamAsync(this IServiceProvider services)
    {
        var persistence = services.GetRequiredService<PublicLogPersistence>();
        var stream = services.GetRequiredService<PublicLogStream>();

        var history = await persistence.LoadAsync();
        stream.SeedHistory(history);
    }
}

public record StationLoggerSource(string Value);