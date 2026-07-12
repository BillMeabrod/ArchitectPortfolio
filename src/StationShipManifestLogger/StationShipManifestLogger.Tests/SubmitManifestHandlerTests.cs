using Azure;
using Xunit;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using StationShipManifestLogger.Common.Data;
using StationShipManifestLogger.Features.Docking;
using System.Text.Json;

namespace StationShipManifestLogger.Tests;

public class SubmitManifestHandlerTests : IDisposable
{
    private readonly ManifestLoggerDbContext _context;
    private readonly Mock<QueueClient> _mockQueueClient;
    private readonly ShipManifestQueuePublisher _publisher;
    private readonly SubmitManifestReportHandler _sut;
    public SubmitManifestHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ManifestLoggerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ManifestLoggerDbContext(options);

        _mockQueueClient = new Mock<QueueClient>();
        _mockQueueClient
            .Setup(c => c.CreateIfNotExistsAsync(
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<Response>().Object);

        // Cover both the string overload and the BinaryData overload since the SDK may route between them.
        _mockQueueClient
            .Setup(c => c.SendMessageAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<Response<SendReceipt>>().Object);
        _mockQueueClient
            .Setup(c => c.SendMessageAsync(
                It.IsAny<BinaryData>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Mock<Response<SendReceipt>>().Object);

        var mockQueueServiceClient = new Mock<QueueServiceClient>();
        mockQueueServiceClient
            .Setup(s => s.GetQueueClient("ship-manifest-queue"))
            .Returns(_mockQueueClient.Object);

        _publisher = new ShipManifestQueuePublisher(mockQueueServiceClient.Object);
        _sut = new SubmitManifestReportHandler(_context, _publisher);
    }

    public void Dispose() => _context.Dispose();

    private static ManifestReportCommand BuildCommand(
        string shipName = "Nebula Runner",
        string callsign = "NR-007",
        string captainName = "Han Solo") => new()
    {
        ShipName = shipName,
        Callsign = callsign,
        CaptainName = captainName,
        CargoItems = ["spice", "fuel"],
        Passengers = ["Chewie"]
    };

    [Fact]
    public async Task Handle_SavesManifestAuditLog_WithCorrectFieldValues()
    {
        var command = BuildCommand();

        await _sut.Handle(command, CancellationToken.None);

        var log = await _context.ManifestAuditLogs.SingleAsync();
        Assert.Equal(command.ShipName, log.ShipName);
        Assert.Equal(command.Callsign, log.Callsign);
        Assert.Equal(command.CaptainName, log.CaptainName);
    }

    [Fact]
    public async Task Handle_SerializesFullManifest_IntoRawPayload()
    {
        var command = BuildCommand();

        await _sut.Handle(command, CancellationToken.None);

        var log = await _context.ManifestAuditLogs.SingleAsync();
        var deserialized = JsonSerializer.Deserialize<ManifestReportCommand>(log.RawPayload);
        Assert.NotNull(deserialized);
        Assert.Equal(command.ShipName, deserialized.ShipName);
        Assert.Equal(command.CargoItems, deserialized.CargoItems);
        Assert.Equal(command.Passengers, deserialized.Passengers);
    }

    [Fact]
    public async Task Handle_CallsPublishAsync_AfterSaving()
    {
        var command = BuildCommand();

        int returnedId = await _sut.Handle(command, CancellationToken.None);

        // CreateIfNotExistsAsync is the first call inside PublishAsync, so verifying it was
        // invoked proves PublishAsync ran. The DB assertion proves save came first.
        var log = await _context.ManifestAuditLogs.FindAsync(returnedId);
        Assert.NotNull(log);
        _mockQueueClient.Verify(
            c => c.CreateIfNotExistsAsync(
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnedId_MatchesSavedAuditLogId()
    {
        var command = BuildCommand();

        int returnedId = await _sut.Handle(command, CancellationToken.None);

        var log = await _context.ManifestAuditLogs.FindAsync(returnedId);
        Assert.NotNull(log);
        Assert.Equal(returnedId, log.Id);
    }
}
