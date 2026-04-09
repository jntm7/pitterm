using F1.Core.Services;
using F1.Infrastructure.Services;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class RaceResultsServiceTests
{
    [Fact]
    public async Task GetRaceResultsAsync_Fallback_ReturnsOrderedResults()
    {
        IRaceResultsService service = new RaceResultsService();

        var results = await service.GetRaceResultsAsync(2025, null, null);

        Assert.NotEmpty(results);
        Assert.True(results.SequenceEqual(results.OrderBy(result => result.Position)));
    }
}
