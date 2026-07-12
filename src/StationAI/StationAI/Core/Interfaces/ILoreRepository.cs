using StationAI.Core.Models;

namespace StationAI.Core.Interfaces;

public interface ILoreRepository
{
    Task<LoreEntry> SaveAsync(LoreEntry entry);
    Task<IReadOnlyList<LoreEntry>> SaveBulkAsync(IReadOnlyList<LoreEntry> entries);
    Task<LoreEntry?> GetByIdAsync(int id);
    Task<IEnumerable<LoreEntry>> GetAllAsync();
    Task DeleteAsync(int id);
    Task<IEnumerable<LoreEntry>> SearchAsync(string query, int topK = 5);
}