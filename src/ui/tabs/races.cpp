#include "races.hpp"

#include <ftxui/component/component.hpp>
#include <ftxui/dom/elements.hpp>

namespace pitterm::ui {

ftxui::Component createRacesTab(state::AppState& appState) {
    using namespace ftxui;

    return Renderer([] {
        return vbox({
            text("Races") | bold,
            text("Select a season first") | dim
        });
    });
}

}
