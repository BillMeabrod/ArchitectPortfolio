using Moq;
using Xunit;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
using StationAI.Core.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace StationAI.Tests;

public class RiskAssessmentServiceTests
{
    private readonly Mock<ILargeLanguageModelService> _llm = new();
    private readonly Mock<IStationDirectiveRepository> _rulesRepo = new();
    private readonly Mock<ILoreRepository> _loreRepo = new();
    private readonly Mock<IDirectiveTargetRepository> _directiveTargetRepo = new();
    private readonly Mock<ILogger<RiskAssessmentService>> _logger = new();
    private readonly RiskAssessmentService _sut;

    private static readonly ShipManifest TestManifest = new()
    {
        ShipName = "Stargazer",
        Callsign = "SG-01",
        CaptainName = "Picard",
        CargoItems = ["dilithium"],
        Passengers = ["Data"]
    };

    private static string ValidAssessmentJson(int bio = 0, int chem = 0, int sec = 1) =>
        JsonSerializer.Serialize(new RiskAssessment
        {
            BiohazardLevel = bio,
            ChemicalHazardLevel = chem,
            SecurityHazardLevel = sec,
            Recommendation = "All clear.",
            InappropriateContent = false
        });

    public RiskAssessmentServiceTests()
    {
        _rulesRepo.Setup(r => r.GetRules()).ReturnsAsync("No current directive.");
        _directiveTargetRepo.Setup(r => r.GetTargetsAsync()).ReturnsAsync([]);
        _loreRepo.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync([]);

        _sut = new RiskAssessmentService(
            _llm.Object,
            _rulesRepo.Object,
            _loreRepo.Object,
            _directiveTargetRepo.Object,
            _logger.Object);
    }

    [Fact]
    public async Task AssessRisk_RetriesOnce_WhenFirstLlmResponseHasOutOfRangeScores()
    {
        var invalidJson = JsonSerializer.Serialize(new RiskAssessment
        {
            BiohazardLevel = 15,
            ChemicalHazardLevel = 0,
            SecurityHazardLevel = 0,
            Recommendation = "invalid"
        });
        var validJson = ValidAssessmentJson();

        var callCount = 0;
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(() => ++callCount == 1 ? invalidJson : validJson);

        var result = await _sut.AssessRisk(TestManifest);

        Assert.Equal(2, callCount);
        Assert.Equal(1, result.SecurityHazardLevel);
    }

    [Fact]
    public async Task AssessRisk_Throws_WhenBothAttemptsReturnInvalidScores()
    {
        var invalidJson = JsonSerializer.Serialize(new RiskAssessment
        {
            BiohazardLevel = 11,
            ChemicalHazardLevel = 0,
            SecurityHazardLevel = 0,
            Recommendation = "invalid"
        });

        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(invalidJson);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.AssessRisk(TestManifest));
    }

    [Fact]
    public async Task AssessRisk_DeduplicatesLoreEntries_ThatAppearInBothSearchResults()
    {
        var sharedEntry = new LoreEntry { Id = 42, Title = "SharedTitle", Category = "person", Body = new string('x', 50) };
        var directiveTarget = new DirectiveTarget { Target = "Some Target", Type = "person", Concern = "Flagged" };

        _directiveTargetRepo.Setup(r => r.GetTargetsAsync()).ReturnsAsync([directiveTarget]);
        _loreRepo
            .Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync([sharedEntry]);

        string? capturedPrompt = null;
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .Callback<string, Type>((p, _) => capturedPrompt = p)
            .ReturnsAsync(ValidAssessmentJson());

        await _sut.AssessRisk(TestManifest);

        Assert.NotNull(capturedPrompt);
        var titleOccurrences = System.Text.RegularExpressions.Regex.Matches(capturedPrompt, "SharedTitle").Count;
        Assert.Equal(1, titleOccurrences);
    }

    [Fact]
    public async Task AssessRisk_ProceedsNormally_WhenLoreSearchThrows()
    {
        _loreRepo
            .Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Qdrant unavailable"));

        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(ValidAssessmentJson());

        var result = await _sut.AssessRisk(TestManifest);

        Assert.NotNull(result);
    }
}
