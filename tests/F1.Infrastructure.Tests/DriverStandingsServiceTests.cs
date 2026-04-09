using F1.Core.Services;
using F1.Infrastructure.Services;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class DriverStandingsServiceTests
{
    [Fact]
    public async Task GetDriverStandingsAsync_Fallback_ReturnsSortedStandings()
    {
        IDriverStandingsService service = new DriverStandingsService();

        var standings = await service.GetDriverStandingsAsync(2025);

        Assert.NotEmpty(standings);
        Assert.True(standings.SequenceEqual(standings.OrderBy(s => s.Position)));
    }
}
