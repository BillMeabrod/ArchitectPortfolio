using Microsoft.AspNetCore.Mvc;
using Station.Logging;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
using StationAI.Core.Models.Validation;
using System.ComponentModel.DataAnnotations;

namespace StationAI.Adapters.Inbound;

[ApiController]
[Route("api/[controller]")]
public class LoreController : ControllerBase
{
    private readonly ILoreService _loreService;
    private readonly IStationLogger<LoreController> _log;

    public LoreController(ILoreService loreService, IStationLogger<LoreController> log)
    {
        _loreService = loreService;
        _log = log;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoreEntry>>> GetAll()
    {
        try
        {
            return Ok(await _loreService.GetAllAsync());
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to retrieve lore entries");
            return StatusCode(500, "Failed to retrieve lore entries");
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LoreEntry>> GetById(int id)
    {
        try
        {
            var entry = await _loreService.GetByIdAsync(id);
            return entry is null ? NotFound($"Lore entry {id} not found") : Ok(entry);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to retrieve lore entry {Id}", id);
            return StatusCode(500, $"Failed to retrieve lore entry {id}");
        }
    }

    [HttpPost]
    public async Task<ActionResult<LoreEntry>> Create(LoreEntryRequest request)
    {
        try
        {
            var created = await _loreService.CreateAsync(request.Title, request.Category, request.Body);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to create lore entry with title {Title}", request.Title);
            return StatusCode(500, "Failed to create lore entry");
        }
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<BulkLoreImportResult>> BulkCreate([FromBody] List<LoreEntryRequest> requests)
    {
        if (requests.Count == 0)
            return BadRequest("At least one entry is required.");

        if (requests.Count > 100)
            return BadRequest("Bulk import is limited to 100 entries per request.");

        try
        {
            var entries = requests.Select(r => (r.Title, r.Category, r.Body)).ToList();
            var result = await _loreService.BulkCreateAsync(entries);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Bulk lore import failed.");
            return StatusCode(500, "Bulk import failed.");
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<LoreEntry>> Update(int id, LoreEntryRequest request)
    {
        try
        {
            var updated = await _loreService.UpdateAsync(id, request.Title, request.Category, request.Body);
            return updated is null ? NotFound($"Lore entry {id} not found") : Ok(updated);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to update lore entry {Id}", id);
            return StatusCode(500, $"Failed to update lore entry {id}");
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _loreService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound($"Lore entry {id} not found");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to delete lore entry {Id}", id);
            return StatusCode(500, $"Failed to delete lore entry {id}");
        }
    }
}

public record LoreEntryRequest(
    [Required][MaxLength(200)] string Title,
    [Required][ValidLoreCategory] string Category,
    [Required][MinLength(50)][MaxLength(8000)] string Body
);