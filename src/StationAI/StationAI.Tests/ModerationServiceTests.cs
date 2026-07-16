using Moq;
using Xunit;
using StationAI.Core.Interfaces;
using StationAI.Core.Services;
using Station.Logging;

namespace StationAI.Tests;

public class ModerationServiceTests
{
    private readonly Mock<ILargeLanguageModelService> _llm = new();
    private readonly Mock<IStationLogger<ModerationService>> _log = new();
    private readonly ModerationService _sut;

    public ModerationServiceTests()
    {
        _sut = new ModerationService(_llm.Object, _log.Object);
    }

    [Fact]
    public async Task IsRejectedByModerationAsync_ReturnsFalse_WhenLlmThrows()
    {
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ThrowsAsync(new Exception("LLM unavailable"));

        var result = await _sut.IsRejectedByModerationAsync("some text");

        Assert.False(result);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("not json at all")]
    [InlineData("{ \"wrong_key\": true }")]
    public async Task IsRejectedByModerationAsync_ReturnsFalse_WhenLlmReturnsUnparseableResponse(string response)
    {
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(response);

        var result = await _sut.IsRejectedByModerationAsync("some text");

        Assert.False(result);
    }

    [Fact]
    public async Task IsRejectedByModerationAsync_ReturnsTrue_WhenLlmReturnsInappropriateTrue()
    {
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync("{ \"inappropriate\": true }");

        var result = await _sut.IsRejectedByModerationAsync("offensive content");

        Assert.True(result);
    }

    [Fact]
    public async Task IsRejectedByModerationAsync_ReturnsFalse_WhenLlmReturnsInappropriateFalse()
    {
        _llm
            .Setup(s => s.SendPrompt(It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync("{ \"inappropriate\": false }");

        var result = await _sut.IsRejectedByModerationAsync("fine content");

        Assert.False(result);
    }
}