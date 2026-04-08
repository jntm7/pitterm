using F1.Core.State;

namespace F1Tui.State;

public interface IAppStateStore
{
    AppState Current { get; }
    void Update(Func<AppState, AppState> update);
}
