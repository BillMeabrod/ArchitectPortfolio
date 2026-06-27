using Npgsql;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;

namespace StationAI.Adapters.Outbound;

public class QdrantLoreAdapter : ILoreRepository
{
    private readonly string _dbConnectionString;
    private readonly QdrantClient _qdrant;
    private readonly IEmbeddingService _embeddingService;
    private readonly string _collectionName;

    private const int VectorSize = 3072;
    private const float SimilarityThreshold = 0.65f;

    public QdrantLoreAdapter(string dbConnectionString, string qdrantUrl, string qdrantApiKey, string collectionName, IEmbeddingService embeddingService)
    {
        _dbConnectionString = NormalizeConnectionString(dbConnectionString);
        _embeddingService = embeddingService;
        _collectionName = collectionName;

        var uri = new Uri(qdrantUrl);
        _qdrant = new QdrantClient(
            host: uri.Host,
            https: true,
            apiKey: qdrantApiKey
        );
    }

    public async Task<LoreEntry> SaveAsync(LoreEntry entry)
    {
        await EnsureCollectionExistsAsync();

        await using var conn = new NpgsqlConnection(_dbConnectionString);
        await conn.OpenAsync();

        if (entry.Id == 0)
        {
            var cmd = new NpgsqlCommand(
                """
                INSERT INTO lore_entry (title, category, body, created_at, updated_at)
                VALUES (@title, @category, @body, NOW(), NOW())
                RETURNING id, created_at, updated_at
                """, conn);

            cmd.Parameters.AddWithValue("title", entry.Title);
            cmd.Parameters.AddWithValue("category", entry.Category);
            cmd.Parameters.AddWithValue("body", entry.Body);

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            entry.Id = reader.GetInt32(0);
            entry.CreatedAt = reader.GetFieldValue<DateTimeOffset>(1);
            entry.UpdatedAt = reader.GetFieldValue<DateTimeOffset>(2);
        }
        else
        {
            var cmd = new NpgsqlCommand(
                """
                UPDATE lore_entry
                SET title = @title, category = @category, body = @body, updated_at = NOW()
                WHERE id = @id
                RETURNING updated_at
                """, conn);

            cmd.Parameters.AddWithValue("id", entry.Id);
            cmd.Parameters.AddWithValue("title", entry.Title);
            cmd.Parameters.AddWithValue("category", entry.Category);
            cmd.Parameters.AddWithValue("body", entry.Body);

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            entry.UpdatedAt = reader.GetFieldValue<DateTimeOffset>(0);
        }

        var vector = await _embeddingService.GetEmbeddingAsync($"{entry.Title}. {entry.Body}");

        await _qdrant.UpsertAsync(_collectionName, [
            new PointStruct
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
            }
        ]);

        return entry;
    }

    public async Task<LoreEntry?> GetByIdAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_dbConnectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, title, category, body, created_at, updated_at FROM lore_entry WHERE id = @id",
            conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return MapRow(reader);
    }

    public async Task<IEnumerable<LoreEntry>> GetAllAsync()
    {
        await using var conn = new NpgsqlConnection(_dbConnectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand(
            "SELECT id, title, category, body, created_at, updated_at FROM lore_entry ORDER BY created_at DESC",
            conn);

        var entries = new List<LoreEntry>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            entries.Add(MapRow(reader));

        return entries;
    }

    public async Task DeleteAsync(int id)
    {
        await using var conn = new NpgsqlConnection(_dbConnectionString);
        await conn.OpenAsync();

        var cmd = new NpgsqlCommand("DELETE FROM lore_entry WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();

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

    private static LoreEntry MapRow(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt32(0),
        Title = reader.GetString(1),
        Category = reader.GetString(2),
        Body = reader.GetString(3),
        CreatedAt = reader.GetFieldValue<DateTimeOffset>(4),
        UpdatedAt = reader.GetFieldValue<DateTimeOffset>(5)
    }; 
    
    private static string NormalizeConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgresql://") && !connectionString.StartsWith("postgres://"))
            return connectionString; // already key-value format

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var builder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : "",
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = Npgsql.SslMode.Require
        };
        return builder.ConnectionString;
    }
}