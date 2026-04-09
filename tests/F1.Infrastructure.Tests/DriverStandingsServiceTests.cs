using F1.Core.Services;
using F1.Infrastructure.Services;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class DriverStandingsServiceTests
{
    [Fact]
    public async Task GetDriverStandingsAsync_WithoutClient_ReturnsNoRows()
    {
        IDriverStandingsService service = new DriverStandingsService();

        var standings = await service.GetDriverStandingsAsync(2025);

        Assert.Empty(standings);
    }
}
