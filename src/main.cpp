#include <ftxui/component/screen_interactive.hpp>

#include "state/state.hpp"
#include "ui/app.hpp"

int main() {
    pitterm::state::AppState appState;
    auto app = pitterm::ui::createApp(appState);
    
    auto screen = ftxui::ScreenInteractive::TerminalOutput();
    screen.Loop(app);
    
    return 0;
}
