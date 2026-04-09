using F1.Core.Services;
using F1.Infrastructure.Services;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class RaceServiceTests
{
    [Fact]
    public async Task GetRacesBySeasonAsync_WithoutClient_ReturnsNoRows()
    {
        IRaceService service = new RaceService();

        var races = await service.GetRacesBySeasonAsync(2025);

        Assert.Empty(races);
    }
}
