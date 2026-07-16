using Xunit;
using Azure.Storage.Queues;
using Microsoft.EntityFrameworkCore;
using Moq;
using Station.Logging;
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

        _mockQueueClient = new Mock<QueueClient>(MockBehavior.Loose);

        var mockQueueServiceClient = new Mock<QueueServiceClient>();
        mockQueueServiceClient
            .Setup(s => s.GetQueueClient("ship-manifest-queue"))
            .Returns(_mockQueueClient.Object);

        _publisher = new ShipManifestQueuePublisher(mockQueueServiceClient.Object);

        _sut = new SubmitManifestReportHandler(_context, _publisher, new Mock<IStationLogger<SubmitManifestReportHandler>>().Object);
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
    public async Task Handle_ReturnedId_MatchesSavedAuditLogId()
    {
        var command = BuildCommand();

        int returnedId = await _sut.Handle(command, CancellationToken.None);

        var log = await _context.ManifestAuditLogs.FindAsync(returnedId);
        Assert.NotNull(log);
        Assert.Equal(returnedId, log.Id);
    }
}