#include "seasons.hpp"

namespace pitterm::ui {

ftxui::Component createSeasonsTab(state::AppState& appState) {
    using namespace ftxui;
    return Renderer([] {
        return vbox({
            text("Seasons Tab - Select a year") | bold,
            text("Use ↑↓ to navigate, Enter to select") | dim
        });
    });
}

}
