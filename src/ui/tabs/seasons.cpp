#include "seasons.hpp"

#include <ftxui/component/component.hpp>
#include <ftxui/dom/elements.hpp>

namespace pitterm::ui {

ftxui::Component createSeasonsTab(state::AppState& appState) {
    using namespace ftxui;

    std::vector<std::string> yearStrings;
    for (int year : appState.available_years) {
        yearStrings.push_back(std::to_string(year));
    }

    auto yearSelector = Menu(&yearStrings, &appState.selected_year_index, MenuOption::VerticalAnimated());

    auto renderer = Renderer(yearSelector, [&] {
        return vbox({
            text("Select Season") | bold | center,
            separator(),
            yearSelector->Render() | center,
            separator(),
            text("Selected: " + std::to_string(appState.get_selected_year())) | dim | center
        });
    });

    return renderer;
}

}
