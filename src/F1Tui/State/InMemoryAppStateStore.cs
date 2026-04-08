using F1.Core.State;

namespace F1Tui.State;

public sealed class InMemoryAppStateStore : IAppStateStore
{
    private AppState current = new();

    public AppState Current => current;

    public void Update(Func<AppState, AppState> update)
    {
        current = update(current);
    }
}
