using F1.Core.Services;
using F1.Infrastructure.Services;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class SeasonServiceTests
{
    [Fact]
    public async Task GetSeasonsAsync_ReturnsDescendingSeasons()
    {
        ISeasonService service = new SeasonService();

        var seasons = await service.GetSeasonsAsync();

        Assert.NotEmpty(seasons);
        Assert.True(seasons.SequenceEqual(seasons.OrderByDescending(s => s.Year)));
    }
}
