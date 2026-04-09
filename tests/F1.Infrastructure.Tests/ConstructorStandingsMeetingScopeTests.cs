using F1.Core.Services;
using F1.Infrastructure.OpenF1;
using F1.Infrastructure.OpenF1.Models;
using F1.Infrastructure.Services;
using Moq;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class ConstructorStandingsMeetingScopeTests
{
    [Fact]
    public async Task GetConstructorStandingsAsync_UsesRequestedMeetingIfPresent()
    {
        var dtos = new List<OpenF1ConstructorStandingDto>
        {
            new() { MeetingKey = 1201, PositionCurrent = 1, TeamName = "Team X", PointsCurrent = 120 },
            new() { MeetingKey = 1202, PositionCurrent = 1, TeamName = "Team X", PointsCurrent = 150 }
        };

        var client = new Mock<IOpenF1Client>();
        client.Setup(c => c.GetConstructorStandingsAsync(2025, 1202, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtos);

        IConstructorStandingsService service = new ConstructorStandingsService(client.Object);

        var standings = await service.GetConstructorStandingsAsync(2025, 1202);

        Assert.Single(standings);
        Assert.Equal(150, standings[0].Points);
    }
}
