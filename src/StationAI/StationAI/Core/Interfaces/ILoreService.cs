using StationAI.Core.Models;

namespace StationAI.Core.Interfaces;

public interface ILoreService
{
    Task<LoreEntry> CreateAsync(string title, string category, string body);
    Task<LoreEntry?> GetByIdAsync(int id);
    Task<IEnumerable<LoreEntry>> GetAllAsync();
    Task<LoreEntry?> UpdateAsync(int id, string title, string category, string body);
    Task<bool> DeleteAsync(int id);
    Task<BulkLoreImportResult> BulkCreateAsync(IReadOnlyList<(string Title, string Category, string Body)> entries);
}