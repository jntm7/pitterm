#include "races.hpp"

namespace pitterm::ui {

ftxui::Component createRacesTab(state::AppState& appState) {
    using namespace ftxui;
    return Renderer([] {
        return vbox({
            text("Races Tab - Select a Grand Prix") | bold,
            text("Select a season first") | dim
        });
    });
}

}
