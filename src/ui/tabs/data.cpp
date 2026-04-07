#include "data.hpp"

namespace pitterm::ui {

ftxui::Component createDataTab(state::AppState& appState) {
    using namespace ftxui;
    return Renderer([] {
        return vbox({
            text("Data Tab - View Lap Times, Stints, Pit Stops") | bold,
            text("Select a session first") | dim
        });
    });
}

}
