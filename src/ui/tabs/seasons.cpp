#include "seasons.hpp"

#include <ftxui/component/component.hpp>
#include <ftxui/dom/elements.hpp>

namespace pitterm::ui {

ftxui::Component createSeasonsTab(state::AppState& appState) {
    using namespace ftxui;

    static std::vector<std::string> yearStrings = {"2026", "2025", "2024", "2023"};
    
    return Menu(&yearStrings, &appState.selected_year_index, MenuOption::VerticalAnimated());
}

}
