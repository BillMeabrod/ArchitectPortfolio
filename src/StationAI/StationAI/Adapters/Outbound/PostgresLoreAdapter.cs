using Npgsql;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;

namespace StationAI.Adapters.Outbound;

public class PostgresLoreAdapter : ILoreStoreRepository
{
    private readonly string _dbConnectionString;

    public PostgresLoreAdapter(string dbConnectionString)
    {
        _dbConnectionString = NormalizeConnectionString(dbConnectionString);
    }

    public async Task<LoreEntry> SaveAsync(LoreEntry entry)
    {
        await using var conn = new NpgsqlConnection(_dbConnectionString);
        await conn.OpenAsync();
        return await UpsertPostgresAsync(conn, entry);
    }

    public async Task<IReadOnlyList<LoreEntry>> SaveBulkAsync(IReadOnlyList<LoreEntry> entries)
    {
        await using var conn = new NpgsqlConnection(_dbConnectionString);
        await conn.OpenAsync();

        var saved = new List<LoreEntry>(entries.Count);
        await using var tx = await conn.BeginTransactionAsync();
        foreach (var entry in entries)
            saved.Add(await UpsertPostgresAsync(conn, entry, tx));
        await tx.CommitAsync();

        return saved;
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
    }

    private static async Task<LoreEntry> UpsertPostgresAsync(
        NpgsqlConnection conn,
        LoreEntry entry,
        NpgsqlTransaction? tx = null)
    {
        if (entry.Id == 0)
        {
            var cmd = new NpgsqlCommand(
                """
                INSERT INTO lore_entry (title, category, body, created_at, updated_at)
                VALUES (@title, @category, @body, NOW(), NOW())
                RETURNING id, created_at, updated_at
                """, conn, tx);

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
                """, conn, tx);

            cmd.Parameters.AddWithValue("id", entry.Id);
            cmd.Parameters.AddWithValue("title", entry.Title);
            cmd.Parameters.AddWithValue("category", entry.Category);
            cmd.Parameters.AddWithValue("body", entry.Body);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException($"Lore entry with ID {entry.Id} does not exist.");
            entry.UpdatedAt = reader.GetFieldValue<DateTimeOffset>(0);
        }

        return entry;
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
            return connectionString;

        var uri = new Uri(connectionString);
        var username = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]);
        var password = uri.UserInfo.Contains(':')
            ? Uri.UnescapeDataString(uri.UserInfo.Substring(uri.UserInfo.IndexOf(':') + 1))
            : "";

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = username,
            Password = password,
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require
        };
        return builder.ConnectionString;
    }
}
