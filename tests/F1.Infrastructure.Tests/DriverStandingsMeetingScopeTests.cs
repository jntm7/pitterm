using F1.Core.Services;
using F1.Infrastructure.OpenF1;
using F1.Infrastructure.OpenF1.Models;
using F1.Infrastructure.Services;
using Moq;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class DriverStandingsMeetingScopeTests
{
    [Fact]
    public async Task GetDriverStandingsAsync_UsesRequestedMeetingIfPresent()
    {
        var dtos = new List<OpenF1DriverStandingDto>
        {
            new() { MeetingKey = 1201, DriverNumber = 1, PositionCurrent = 1, PointsCurrent = 50 },
            new() { MeetingKey = 1202, DriverNumber = 1, PositionCurrent = 1, PointsCurrent = 75 }
        };

        var drivers = new List<OpenF1DriverDto>
        {
            new() { DriverNumber = 1, FullName = "Driver A", TeamName = "Team X" }
        };

        var client = new Mock<IOpenF1Client>();
        client.Setup(c => c.GetDriverStandingsAsync(2025, 1202, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtos);
        client.Setup(c => c.GetDriversAsync(1202, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(drivers);

        IDriverStandingsService service = new DriverStandingsService(client.Object);

        var standings = await service.GetDriverStandingsAsync(2025, 1202);

        Assert.Single(standings);
        Assert.Equal(75, standings[0].Points);
    }
}
