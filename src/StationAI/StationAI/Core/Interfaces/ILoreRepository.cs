using StationAI.Core.Models;

namespace StationAI.Core.Interfaces;

public interface ILoreRepository
{
    Task UpsertAsync(LoreEntry entry);
    Task UpsertBulkAsync(IReadOnlyList<LoreEntry> entries);
    Task DeleteAsync(int id);
    Task<IEnumerable<LoreEntry>> SearchAsync(string query, int topK = 5);
}
