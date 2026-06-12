using Azure.Storage.Queues;
using StationAI.Core.Models;
using System.Text;
using System.Text.Json;

namespace StationAI.Functions
{
    public class RiskAssessmentQueuePublisher
    {
        private readonly QueueClient _queueClient;

        public RiskAssessmentQueuePublisher(QueueServiceClient queueServiceClient)
        {
            _queueClient = queueServiceClient.GetQueueClient("risk-assessment-queue");
        }

        public async Task PublishAsync(RiskAssessment assessment, ShipManifest manifest)
        {
            await _queueClient.CreateIfNotExistsAsync();

            var payload = new
            {
                Manifest = manifest,
                Assessment = assessment
            };

            var json = JsonSerializer.Serialize(payload);
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            await _queueClient.SendMessageAsync(base64);
        }
    }
}
