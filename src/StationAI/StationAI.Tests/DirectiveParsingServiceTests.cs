using Moq;
using Xunit;
using StationAI.Core.Interfaces;
using StationAI.Core.Models;
using StationAI.Core.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace StationAI.Tests;

public class DirectiveParsingServiceTests
{
    private readonly Mock<ILargeLanguageModelService> _llm = new();
    private readonly Mock<ILogger<DirectiveParsingService>> _logger = new();
    private readonly DirectiveParsingService _sut;

    public DirectiveParsingServiceTests()
    {
        _sut = new DirectiveParsingService(_llm.Object, _logger.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task Parse_ReturnsEmptyList_ForWhitespaceInput(string input)
    {
        var result = await _sut.Parse(input);

        Assert.Empty(result);
        _llm.Verify(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()), Times.Never);
    }

    [Fact]
    public async Task Parse_ReturnsEmptyList_IfLlmCallThrows()
    {
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ThrowsAsync(new Exception("LLM down"));

        var result = await _sut.Parse("Watch out for the Crimson Syndicate.");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Parse_ReturnsEmptyList_IfLlmReturnsMalformedJson()
    {
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync("this is not json at all {{{");

        var result = await _sut.Parse("Some directive text.");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Parse_DiscardsTargets_WithEmptyTargetField()
    {
        var targets = new[]
        {
            new DirectiveTarget { Target = "", Type = "person", Concern = "unknown" }
        };
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(JsonSerializer.Serialize(targets));

        var result = await _sut.Parse("Some directive.");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Parse_DiscardsTargets_WithUnrecognizedCategory()
    {
        var targets = new[]
        {
            new DirectiveTarget { Target = "Zeta Station", Type = "spaceship", Concern = "suspicious" }
        };
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(JsonSerializer.Serialize(targets));

        var result = await _sut.Parse("Watch Zeta Station.");

        Assert.Empty(result);
    }

    [Fact]
    public async Task Parse_NormalizesValidCategory_ToLowercase()
    {
        var targets = new[]
        {
            new DirectiveTarget { Target = "Crimson Syndicate", Type = "FACTION", Concern = "hostile" }
        };
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(JsonSerializer.Serialize(targets));

        var result = await _sut.Parse("Beware the Crimson Syndicate.");

        Assert.Single(result);
        Assert.Equal("faction", result[0].Type);
    }
}
