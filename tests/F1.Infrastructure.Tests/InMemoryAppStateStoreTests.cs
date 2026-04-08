using F1Tui.State;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class InMemoryAppStateStoreTests
{
    [Fact]
    public void Update_ChangesSelectedSeasonAndStatusMessage()
    {
        var store = new InMemoryAppStateStore();

        store.Update(state => state with
        {
            SelectedSeason = 2025,
            SelectedGrandPrixName = "Australian Grand Prix",
            StatusMessage = "Loaded 10 seasons"
        });

        Assert.Equal(2025, store.Current.SelectedSeason);
        Assert.Equal("Australian Grand Prix", store.Current.SelectedGrandPrixName);
        Assert.Equal("Loaded 10 seasons", store.Current.StatusMessage);
    }
}
