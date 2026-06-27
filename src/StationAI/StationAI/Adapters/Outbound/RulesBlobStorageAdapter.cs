using Azure.Storage.Blobs;
using StationAI.Core.Interfaces;

namespace StationAI.Adapters.Outbound
{
    public class RulesBlobStorageAdapter : IRulesRepository
    {
        private readonly BlobContainerClient _containerClient;
        private readonly BlobClient _blobClient;

        public RulesBlobStorageAdapter(BlobServiceClient blobServiceClient)
        {
            _containerClient = blobServiceClient.GetBlobContainerClient("station-rules");
            _blobClient = _containerClient.GetBlobClient("current-rules.txt");
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
            await _containerClient.CreateIfNotExistsAsync();

            await _blobClient.UploadAsync(
                BinaryData.FromString(rules), overwrite: true);
        }
    }
}