using StationAI.Adapters.Inbound;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
using StationAI.Core.Models.Constants;

namespace StationAI.Core.Services;

public class LoreService : ILoreService
{
    private readonly ILoreRepository _loreRepository;
    private readonly ILogger<LoreService> _logger;

    public LoreService(ILoreRepository loreRepository, ILogger<LoreService> logger)
    {
        _loreRepository = loreRepository;
        _logger = logger;
    }

    public async Task<LoreEntry> CreateAsync(string title, string category, string body)
    {
        var entry = new LoreEntry
        {
            Title = title,
            Category = category.ToLowerInvariant(),
            Body = body
        };

        return await _loreRepository.SaveAsync(entry);
    }

    public Task<LoreEntry?> GetByIdAsync(int id) =>
        _loreRepository.GetByIdAsync(id);

    public Task<IEnumerable<LoreEntry>> GetAllAsync() =>
        _loreRepository.GetAllAsync();

    public async Task<LoreEntry?> UpdateAsync(int id, string title, string category, string body)
    {
        var existing = await _loreRepository.GetByIdAsync(id);
        if (existing is null)
            return null;

        existing.Title = title;
        existing.Category = category.ToLowerInvariant();
        existing.Body = body;

        return await _loreRepository.SaveAsync(existing);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _loreRepository.GetByIdAsync(id);
        if (existing is null)
            return false;

        await _loreRepository.DeleteAsync(id);
        return true;
    }

    public async Task<BulkImportResult> BulkCreateAsync(IReadOnlyList<(string Title, string Category, string Body)> entries)
    {
        var result = new BulkImportResult();
        var toSave = new List<LoreEntry>(entries.Count);

        foreach (var (title, category, body) in entries)
        {
            var errors = Validate(title, category, body);
            if (errors.Count > 0)
            {
                result.Failures.Add(new BulkImportFailure(title, string.Join("; ", errors)));
                continue;
            }

            toSave.Add(new LoreEntry
            {
                Title = title,
                Category = category.ToLowerInvariant(),
                Body = body
            });
        }

        if (toSave.Count > 0)
        {
            var saved = await _loreRepository.SaveBulkAsync(toSave);
            result.Succeeded = saved.Count;

            _logger.LogInformation(
                "Bulk lore import complete: {Succeeded} saved, {Failed} failed.",
                result.Succeeded, result.Failures.Count);
        }

        return result;
    }

    private static List<string> Validate(string title, string category, string body)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(title) || title.Length > 200)
            errors.Add("Title is required and must be 200 characters or fewer");

        if (!LoreCategories.IsValid(category))
            errors.Add($"Category '{category}' is not valid. Must be one of: {string.Join(", ", LoreCategories.All)}");

        if (string.IsNullOrWhiteSpace(body) || body.Length < 50 || body.Length > 8000)
            errors.Add("Body is required and must be between 50 and 8000 characters");

        return errors;
    }
}