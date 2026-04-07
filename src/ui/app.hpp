#pragma once

#include <ftxui/component/component.hpp>
#include <ftxui/dom/elements.hpp>

#include "../state/state.hpp"

namespace pitterm::ui {

ftxui::Component createApp(state::AppState& appState);

}
