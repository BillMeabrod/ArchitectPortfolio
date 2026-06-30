using Microsoft.AspNetCore.Mvc;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
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
    public async Task<ActionResult<LoreEntry>> Create(CreateLoreEntryRequest request)
    {
        try
        {
            var entry = new LoreEntry
            {
                Title = request.Title,
                Category = request.Category,
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

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LoreEntry>> Update(int id, UpdateLoreEntryRequest request)
    {
        try
        {
            var existing = await _loreRepository.GetByIdAsync(id);
            if (existing is null)
                return NotFound($"Lore entry {id} not found");

            existing.Title = request.Title;
            existing.Category = request.Category;
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
}

public record CreateLoreEntryRequest(
    [Required][MaxLength(200)] string Title,
    [Required][ValidLoreCategory] string Category,
    [Required][MinLength(50)][MaxLength(8000)] string Body
);

public record UpdateLoreEntryRequest(
    [Required][MaxLength(200)] string Title,
    [Required][ValidLoreCategory] string Category,
    [Required][MinLength(50)][MaxLength(8000)] string Body
);