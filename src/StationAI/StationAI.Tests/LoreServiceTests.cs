using Moq;
using Xunit;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
using StationAI.Core.Services;
using Microsoft.Extensions.Logging;

namespace StationAI.Tests;

public class LoreServiceTests
{
    private readonly Mock<ILoreStoreRepository> _loreStore = new();
    private readonly Mock<ILoreRepository> _loreRepo = new();
    private readonly Mock<ILogger<LoreService>> _logger = new();
    private readonly LoreService _sut;

    public LoreServiceTests()
    {
        _sut = new LoreService(_loreStore.Object, _loreRepo.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateAsync_NormalizesCategory_ToLowercase()
    {
        var saved = new LoreEntry { Id = 1, Title = "T", Category = "person", Body = new string('x', 50) };
        LoreEntry? capturedEntry = null;

        _loreStore
            .Setup(r => r.SaveAsync(It.IsAny<LoreEntry>()))
            .Callback<LoreEntry>(e => capturedEntry = e)
            .ReturnsAsync(saved);
        _loreRepo
            .Setup(r => r.UpsertAsync(It.IsAny<LoreEntry>()))
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync("T", "PERSON", new string('x', 50));

        Assert.NotNull(capturedEntry);
        Assert.Equal("person", capturedEntry.Category);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenEntryDoesNotExist()
    {
        _loreStore.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((LoreEntry?)null);

        var result = await _sut.UpdateAsync(99, "T", "person", new string('x', 50));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenEntryDoesNotExist()
    {
        _loreStore.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((LoreEntry?)null);

        var result = await _sut.DeleteAsync(99);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_AttemptsToRestoreQdrantEntry_WhenPostgresDeleteFails()
    {
        var existing = new LoreEntry { Id = 7, Title = "T", Category = "person", Body = new string('x', 50) };

        _loreStore.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(existing);
        _loreRepo.Setup(r => r.DeleteAsync(7)).Returns(Task.CompletedTask);
        _loreStore.Setup(r => r.DeleteAsync(7)).ThrowsAsync(new Exception("Postgres failure"));
        _loreRepo.Setup(r => r.UpsertAsync(existing)).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<Exception>(() => _sut.DeleteAsync(7));

        _loreRepo.Verify(r => r.UpsertAsync(existing), Times.Once);
    }

    [Fact]
    public async Task BulkCreateAsync_CollectsValidationFailures_WithoutStoppingValidEntries()
    {
        var validBody = new string('x', 50);
        var entries = new List<(string Title, string Category, string Body)>
        {
            ("Valid Title", "person", validBody),
            ("", "person", validBody),
        };

        var savedValid = new LoreEntry { Id = 1, Title = "Valid Title", Category = "person", Body = validBody };
        _loreStore
            .Setup(r => r.SaveBulkAsync(It.IsAny<IReadOnlyList<LoreEntry>>()))
            .ReturnsAsync([savedValid]);
        _loreRepo
            .Setup(r => r.UpsertBulkAsync(It.IsAny<IReadOnlyList<LoreEntry>>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.BulkCreateAsync(entries);

        Assert.Equal(1, result.Succeeded);
        Assert.Single(result.Failures);
    }

    [Fact]
    public async Task BulkCreateAsync_RejectsEntries_WithInvalidCategory()
    {
        var entries = new List<(string, string, string)>
        {
            ("Title", "notacategory", new string('x', 50))
        };

        _loreStore
            .Setup(r => r.SaveBulkAsync(It.IsAny<IReadOnlyList<LoreEntry>>()))
            .ReturnsAsync([]);
        _loreRepo
            .Setup(r => r.UpsertBulkAsync(It.IsAny<IReadOnlyList<LoreEntry>>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.BulkCreateAsync(entries);

        Assert.Equal(0, result.Succeeded);
        Assert.Single(result.Failures);
        Assert.Contains("notacategory", result.Failures[0].Reason);
    }

    [Fact]
    public async Task BulkCreateAsync_RejectsEntries_WhereBodyIsUnder50Characters()
    {
        var entries = new List<(string, string, string)>
        {
            ("Title", "person", "too short")
        };

        _loreStore
            .Setup(r => r.SaveBulkAsync(It.IsAny<IReadOnlyList<LoreEntry>>()))
            .ReturnsAsync([]);
        _loreRepo
            .Setup(r => r.UpsertBulkAsync(It.IsAny<IReadOnlyList<LoreEntry>>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.BulkCreateAsync(entries);

        Assert.Equal(0, result.Succeeded);
        Assert.Single(result.Failures);
    }

    [Fact]
    public async Task BulkCreateAsync_RejectsEntries_WhereTitleIsEmpty()
    {
        var entries = new List<(string, string, string)>
        {
            ("", "person", new string('x', 50))
        };

        _loreStore
            .Setup(r => r.SaveBulkAsync(It.IsAny<IReadOnlyList<LoreEntry>>()))
            .ReturnsAsync([]);
        _loreRepo
            .Setup(r => r.UpsertBulkAsync(It.IsAny<IReadOnlyList<LoreEntry>>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.BulkCreateAsync(entries);

        Assert.Equal(0, result.Succeeded);
        Assert.Single(result.Failures);
    }
}
