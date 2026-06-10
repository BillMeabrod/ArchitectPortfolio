using Azure.Storage.Blobs;
using StationAI.Core.Interfaces;

namespace StationAI.Adapters.Outbound
{
    public class RulesBlobStorageAdapter : IRulesRepository
    {
        private readonly BlobClient _blobClient;

        public RulesBlobStorageAdapter(BlobServiceClient blobServiceClient)
        {
            _blobClient = blobServiceClient
                .GetBlobContainerClient("station-rules")
                .GetBlobClient("current-rules.txt");
        }

        public async Task<string?> GetRules()
        {
            if (!await _blobClient.ExistsAsync())
                return null;

            var response = await _blobClient.DownloadContentAsync();
            return response.Value.Content.ToString();
        }

        public async Task SaveRules(string rules)
        {
            await _blobClient.UploadAsync(
                BinaryData.FromString(rules), overwrite: true);
        }
    }
}
