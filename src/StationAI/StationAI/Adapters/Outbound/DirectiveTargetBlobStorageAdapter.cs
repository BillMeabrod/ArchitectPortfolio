using Azure;
using Azure.Storage.Blobs;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
using System.Text.Json;

namespace StationAI.Adapters.Outbound
{
    public class DirectiveTargetBlobStorageAdapter : IDirectiveTargetRepository
    {
        private readonly BlobContainerClient _containerClient;
        private readonly BlobClient _blobClient;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public DirectiveTargetBlobStorageAdapter(BlobServiceClient blobServiceClient)
        {
            _containerClient = blobServiceClient.GetBlobContainerClient("station-rules");
            _blobClient = _containerClient.GetBlobClient("current-targets.json");
        }

        public async Task<IReadOnlyList<DirectiveTarget>> GetTargetsAsync()
        {
            if (!await _blobClient.ExistsAsync())
                return [];

            Response<Azure.Storage.Blobs.Models.BlobDownloadResult> response =
                await _blobClient.DownloadContentAsync();

            string json = response.Value.Content.ToString();
            if (string.IsNullOrWhiteSpace(json))
                return [];

            var targets = JsonSerializer.Deserialize<List<DirectiveTarget>>(json, SerializerOptions);
            return targets ?? [];
        }

        public async Task SaveTargetsAsync(IReadOnlyList<DirectiveTarget> targets)
        {
            await _containerClient.CreateIfNotExistsAsync();

            string json = JsonSerializer.Serialize(targets, SerializerOptions);
            await _blobClient.UploadAsync(BinaryData.FromString(json), overwrite: true);
        }
    }
}