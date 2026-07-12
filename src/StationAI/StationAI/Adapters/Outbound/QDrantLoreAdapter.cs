using Qdrant.Client;
using Qdrant.Client.Grpc;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;

namespace StationAI.Adapters.Outbound;

public class QdrantLoreAdapter : ILoreRepository
{
    private readonly QdrantClient _qdrant;
    private readonly IEmbeddingService _embeddingService;
    private readonly string _collectionName;

    private const int VectorSize = 3072;
    private const float SimilarityThreshold = 0.65f;

    public QdrantLoreAdapter(string qdrantUrl, string qdrantApiKey, string collectionName, IEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
        _collectionName = collectionName;

        var uri = new Uri(qdrantUrl);
        var isHttps = uri.Scheme == "https";
        var defaultPort = isHttps ? 443 : 80;
        var port = uri.Port > 0 && uri.Port != defaultPort ? uri.Port : defaultPort;

        _qdrant = new QdrantClient(
            host: uri.Host,
            port: port,
            https: isHttps,
            apiKey: qdrantApiKey
        );
    }

    public async Task UpsertAsync(LoreEntry entry)
    {
        await EnsureCollectionExistsAsync();
        if (entry.Id == 0)
            throw new InvalidOperationException("Lore entry must have an ID before upserting to Qdrant.");

        var vector = await _embeddingService.GetEmbeddingAsync($"{entry.Title}. {entry.Body}");
        await _qdrant.UpsertAsync(_collectionName, [BuildPoint(entry, vector)]);
    }

    public async Task UpsertBulkAsync(IReadOnlyList<LoreEntry> entries)
    {
        await EnsureCollectionExistsAsync();

        var vectors = new List<float[]>(entries.Count);
        foreach (var entry in entries)
            vectors.Add(await _embeddingService.GetEmbeddingAsync($"{entry.Title}. {entry.Body}"));
        if (entries.Any(e => e.Id == 0))
            throw new InvalidOperationException("All lore entries must have IDs before upserting to Qdrant.");

        var points = new List<PointStruct>(entries.Count);
        for (var i = 0; i < entries.Count; i++)
            points.Add(BuildPoint(entries[i], vectors[i]));

        await _qdrant.UpsertAsync(_collectionName, points);
    }

    public async Task DeleteAsync(int id)
    {
        await _qdrant.DeleteAsync(_collectionName, (ulong)id);
    }

    public async Task<IEnumerable<LoreEntry>> SearchAsync(string query, int topK = 5)
    {
        await EnsureCollectionExistsAsync();

        var vector = await _embeddingService.GetEmbeddingAsync(query);

        var results = await _qdrant.SearchAsync(
            _collectionName,
            vector,
            limit: (ulong)topK,
            scoreThreshold: SimilarityThreshold
        );

        return results.Select(r => new LoreEntry
        {
            Id = (int)r.Id.Num,
            Title = r.Payload["title"].StringValue,
            Category = r.Payload["category"].StringValue,
            Body = r.Payload["body"].StringValue
        });
    }

    private static PointStruct BuildPoint(LoreEntry entry, float[] vector) => new()
    {
        Id = (ulong)entry.Id,
        Vectors = vector,
        Payload =
        {
            ["postgres_id"] = entry.Id,
            ["title"] = entry.Title,
            ["category"] = entry.Category,
            ["body"] = entry.Body
        }
    };

    private async Task EnsureCollectionExistsAsync()
    {
        var collections = await _qdrant.ListCollectionsAsync();
        if (collections.Any(c => c == _collectionName))
            return;

        await _qdrant.CreateCollectionAsync(_collectionName,
            new VectorParams
            {
                Size = VectorSize,
                Distance = Distance.Cosine
            });
    }

}
