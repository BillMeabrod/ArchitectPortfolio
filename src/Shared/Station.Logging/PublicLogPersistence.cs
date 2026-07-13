using Azure.Storage.Blobs;
using System.Text.Json;

namespace Station.Logging;

public class PublicLogPersistence
{
    private readonly BlobContainerClient _container;
    private readonly BlobClient _blob;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PublicLogPersistence(BlobServiceClient blobServiceClient, string appName)
    {
        _container = blobServiceClient.GetBlobContainerClient("log-streams");
        _blob = _container.GetBlobClient($"{appName}.json");
    }

    public virtual async Task<List<LogEntry>> LoadAsync()
    {
        try
        {
            await _container.CreateIfNotExistsAsync();
            if (!await _blob.ExistsAsync())
                return [];

            var response = await _blob.DownloadContentAsync();
            return JsonSerializer.Deserialize<List<LogEntry>>(
                response.Value.Content.ToString()) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public virtual async Task SaveAsync(IReadOnlyList<LogEntry> entries)
    {
        await _lock.WaitAsync();
        try
        {
            await _container.CreateIfNotExistsAsync();
            var json = JsonSerializer.Serialize(entries);
            await _blob.UploadAsync(BinaryData.FromString(json), overwrite: true);
        }
        catch { }
        finally
        {
            _lock.Release();
        }
    }
}