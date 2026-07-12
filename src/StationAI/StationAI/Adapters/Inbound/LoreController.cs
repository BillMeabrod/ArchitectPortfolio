using Microsoft.AspNetCore.Mvc;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
using StationAI.Core.Models.Constants;
using StationAI.Core.Models.Validation;
using System.ComponentModel.DataAnnotations;

namespace StationAI.Adapters.Inbound;

[ApiController]
[Route("api/[controller]")]
public class LoreController : ControllerBase
{
    private readonly ILoreRepository _loreRepository;
    private readonly ILogger<LoreController> _logger;

    public LoreController(ILoreRepository loreRepository, ILogger<LoreController> logger)
    {
        _loreRepository = loreRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoreEntry>>> GetAll()
    {
        try
        {
            var entries = await _loreRepository.GetAllAsync();
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve lore entries");
            return StatusCode(500, "Failed to retrieve lore entries");
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoreEntry>> GetById(int id)
    {
        try
        {
            var entry = await _loreRepository.GetByIdAsync(id);
            if (entry is null)
                return NotFound($"Lore entry {id} not found");

            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve lore entry {Id}", id);
            return StatusCode(500, $"Failed to retrieve lore entry {id}");
        }
    }

    [HttpPost]
    public async Task<ActionResult<LoreEntry>> Create(LoreEntryRequest request)
    {
        try
        {
            var entry = new LoreEntry
            {
                Title = request.Title,
                Category = request.Category.ToLowerInvariant(),
                Body = request.Body
            };

            var created = await _loreRepository.SaveAsync(entry);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create lore entry with title {Title}", request.Title);
            return StatusCode(500, "Failed to create lore entry");
        }
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<BulkImportResult>> BulkCreate([FromBody] List<LoreEntryRequest> requests)
    {
        if (requests.Count == 0)
            return BadRequest("At least one entry is required.");

        if (requests.Count > 100)
            return BadRequest("Bulk import is limited to 100 entries per request.");

        var result = new BulkImportResult();
        var toSave = new List<LoreEntry>(requests.Count);

        foreach (var request in requests)
        {
            var errors = ValidateEntry(request);
            if (errors.Count > 0)
            {
                result.Failures.Add(new BulkImportFailure(request.Title, string.Join("; ", errors)));
                continue;
            }

            toSave.Add(new LoreEntry
            {
                Title = request.Title,
                Category = request.Category.ToLowerInvariant(),
                Body = request.Body
            });
        }

        if (toSave.Count > 0)
        {
            try
            {
                var saved = await _loreRepository.SaveBulkAsync(toSave);
                result.Succeeded = saved.Count;

                _logger.LogInformation(
                    "Bulk lore import complete: {Succeeded} saved, {Failed} failed.",
                    result.Succeeded, result.Failures.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk lore import failed during storage write.");
                return StatusCode(500, "Bulk import failed during storage write. No entries were saved.");
            }
        }

        return Ok(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LoreEntry>> Update(int id, LoreEntryRequest request)
    {
        try
        {
            var existing = await _loreRepository.GetByIdAsync(id);
            if (existing is null)
                return NotFound($"Lore entry {id} not found");

            existing.Title = request.Title;
            existing.Category = request.Category.ToLowerInvariant();
            existing.Body = request.Body;

            var updated = await _loreRepository.SaveAsync(existing);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update lore entry {Id}", id);
            return StatusCode(500, $"Failed to update lore entry {id}");
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var existing = await _loreRepository.GetByIdAsync(id);
            if (existing is null)
                return NotFound($"Lore entry {id} not found");

            await _loreRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete lore entry {Id}", id);
            return StatusCode(500, $"Failed to delete lore entry {id}");
        }
    }

    private static List<string> ValidateEntry(LoreEntryRequest entry)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(entry.Title) || entry.Title.Length > 200)
            errors.Add("Title is required and must be 200 characters or fewer");

        if (!LoreCategories.IsValid(entry.Category))
            errors.Add($"Category '{entry.Category}' is not valid. Must be one of: {string.Join(", ", LoreCategories.All)}");

        if (string.IsNullOrWhiteSpace(entry.Body) || entry.Body.Length < 50 || entry.Body.Length > 8000)
            errors.Add("Body is required and must be between 50 and 8000 characters");

        return errors;
    }
}

public record LoreEntryRequest(
    [Required][MaxLength(200)] string Title,
    [Required][ValidLoreCategory] string Category,
    [Required][MinLength(50)][MaxLength(8000)] string Body
);

public record BulkImportFailure(string Title, string Reason);

public class BulkImportResult
{
    public int Succeeded { get; set; }
    public List<BulkImportFailure> Failures { get; set; } = [];
}