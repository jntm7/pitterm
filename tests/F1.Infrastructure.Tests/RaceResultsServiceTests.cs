using F1.Core.Services;
using F1.Infrastructure.Services;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class RaceResultsServiceTests
{
    [Fact]
    public async Task GetRaceResultsAsync_WithoutClient_ReturnsNoRows()
    {
        IRaceResultsService service = new RaceResultsService();

        var results = await service.GetRaceResultsAsync(2025, null, null);

        Assert.Empty(results);
    }
}
