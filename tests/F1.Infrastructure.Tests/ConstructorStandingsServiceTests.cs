using F1.Core.Services;
using F1.Infrastructure.Services;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class ConstructorStandingsServiceTests
{
    [Fact]
    public async Task GetConstructorStandingsAsync_Fallback_ReturnsSortedStandings()
    {
        IConstructorStandingsService service = new ConstructorStandingsService();

        var standings = await service.GetConstructorStandingsAsync(2025);

        Assert.NotEmpty(standings);
        Assert.True(standings.SequenceEqual(standings.OrderBy(s => s.Position)));
    }
}
