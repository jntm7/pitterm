#include "app.hpp"

#include <ftxui/component/component.hpp>
#include <ftxui/dom/elements.hpp>

#include "tabs/seasons.hpp"
#include "tabs/races.hpp"
#include "tabs/sessions.hpp"
#include "tabs/data.hpp"

namespace pitterm::ui {

ftxui::Component createApp(state::AppState& appState) {
    using namespace ftxui;

    auto seasonsTab = createSeasonsTab(appState);
    auto racesTab = createRacesTab(appState);
    auto sessionsTab = createSessionsTab(appState);
    auto dataTab = createDataTab(appState);

    std::vector<std::string> tabTitles = {
        "Seasons",
        "Races",
        "Sessions", 
        "Data"
    };

    auto tabBar = Menu(&tabTitles, &appState.selected_tab_index, MenuOption::HorizontalAnimated());

    auto tabContainer = Container::Tab({
        seasonsTab,
        racesTab,
        sessionsTab,
        dataTab
    }, &appState.selected_tab_index);

    auto renderer = Renderer(tabContainer, [&] {
        return vbox({
            text("PitTerm - F1 Historical Data Viewer") | bold | center,
            separator(),
            tabBar->Render() | hcenter,
            separator(),
            tabContainer->Render() | flex,
            separator(),
            text("Arrow keys to navigate, Enter to select, Ctrl+C to exit") | dim | center
        });
    });

    return renderer;
}

}
