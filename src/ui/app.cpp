#include "app.hpp"

#include <ftxui/component/component.hpp>
#include <ftxui/component/screen_interactive.hpp>
#include <ftxui/dom/elements.hpp>

#include "tabs/seasons.hpp"
#include "tabs/races.hpp"
#include "tabs/sessions.hpp"
#include "tabs/data.hpp"

namespace pitterm::ui {

ftxui::Component createApp(state::AppState& appState) {
    using namespace ftxui;

    std::vector<std::string> tabTitles = {
        "Seasons",
        "Races",
        "Sessions",
        "Data"
    };

    auto tabBar = Menu(&tabTitles, &appState.selected_tab_index, MenuOption::HorizontalAnimated());

    auto seasonsTab = createSeasonsTab(appState);
    auto racesTab = createRacesTab(appState);
    auto sessionsTab = createSessionsTab(appState);
    auto dataTab = createDataTab(appState);

    auto container = Container::Vertical({
        tabBar,
        seasonsTab,
        racesTab,
        sessionsTab,
        dataTab
    });

    return Renderer(container, [&] {
        Elements content;
        
        if (appState.selected_tab_index == 0) {
            content.push_back(seasonsTab->Render());
        } else if (appState.selected_tab_index == 1) {
            content.push_back(racesTab->Render());
        } else if (appState.selected_tab_index == 2) {
            content.push_back(sessionsTab->Render());
        } else {
            content.push_back(dataTab->Render());
        }
        
        return vbox({
            tabBar->Render() | border,
            vbox(std::move(content)) | flex
        });
    });
}

}
