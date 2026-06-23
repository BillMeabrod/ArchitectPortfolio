using Google.GenAI;
using StationAI.Core.Interfaces;

namespace StationAI.Adapters.Outbound;

public class GoogleEmbeddingAdapter : IEmbeddingService
{
    private readonly Client _client = new();
    private const string Model = "gemini-embedding-001";

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var response = await _client.Models.EmbedContentAsync(Model, text);
        return response.Embeddings[0].Values.Select(v => (float)v).ToArray();
    }
}