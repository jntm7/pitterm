#include "sessions.hpp"

namespace pitterm::ui {

ftxui::Component createSessionsTab(state::AppState& appState) {
    using namespace ftxui;
    return Renderer([] {
        return vbox({
            text("Sessions Tab - Select Practice/Qualifying/Race") | bold,
            text("Select a race first") | dim
        });
    });
}

}
