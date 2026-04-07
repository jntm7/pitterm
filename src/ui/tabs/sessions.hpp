#pragma once

#include <ftxui/component/component.hpp>
#include "../../state/state.hpp"

namespace pitterm::ui {

ftxui::Component createSessionsTab(state::AppState& appState);

}
