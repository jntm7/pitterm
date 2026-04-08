using F1.Core.Models;
using Xunit;

namespace F1.Core.Tests;

public sealed class SeasonTests
{
    [Fact]
    public void Season_Record_HoldsYear()
    {
        var season = new Season(2024);

        Assert.Equal(2024, season.Year);
    }
}
