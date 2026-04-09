using F1.Infrastructure.OpenF1;
using F1.Infrastructure.OpenF1.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Moq;

namespace F1.Infrastructure.Tests;

public sealed class CachedOpenF1ClientTests
{
    [Fact]
    public async Task GetSessionsAsync_CachesResults_AndReturnsCachedOnSecondCall()
    {
        var yearData = new List<OpenF1SessionDto>
        {
            new() { SessionKey = 1, Year = 2025, SessionName = "Race", MeetingName = "Bahrain Grand Prix", Round = 1 }
        };

        var inner = new Mock<IOpenF1Client>();
        inner.Setup(client => client.GetSessionsAsync(2025, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(yearData.AsReadOnly());

        var options = Options.Create(new OpenF1CacheOptions { CacheTtlMinutes = 30 });
        var logger = new Mock<ILogger<CachedOpenF1Client>>();

        var cached = new CachedOpenF1Client(inner.Object, options, logger.Object);

        var first = await cached.GetSessionsAsync(2025);
        var second = await cached.GetSessionsAsync(2025);

        Assert.Equal(yearData.Count, first.Count);
        Assert.Equal(yearData.Count, second.Count);
        inner.Verify(client => client.GetSessionsAsync(2025, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSessionsAsync_WithDifferentParams_CallsApiSeparately()
    {
        var raceData = new List<OpenF1SessionDto>
        {
            new() { SessionKey = 1, Year = 2025, SessionName = "Race", MeetingName = "Bahrain Grand Prix", Round = 1 }
        };

        var inner = new Mock<IOpenF1Client>();
        inner.Setup(client => client.GetSessionsAsync(2025, "Race", It.IsAny<CancellationToken>()))
            .ReturnsAsync(raceData.AsReadOnly());
        inner.Setup(client => client.GetSessionsAsync(2025, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(raceData.AsReadOnly());

        var options = Options.Create(new OpenF1CacheOptions { CacheTtlMinutes = 30 });
        var logger = new Mock<ILogger<CachedOpenF1Client>>();

        var cached = new CachedOpenF1Client(inner.Object, options, logger.Object);

        await cached.GetSessionsAsync(2025, "Race");
        await cached.GetSessionsAsync(2025);

        inner.Verify(client => client.GetSessionsAsync(2025, "Race", It.IsAny<CancellationToken>()), Times.Once);
        inner.Verify(client => client.GetSessionsAsync(2025, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}