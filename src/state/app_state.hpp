#pragma once

#include <vector>
#include <functional>

#include "session_context.hpp"
#include "cache.hpp"

#include "../data/models.hpp"

namespace pitterm::state {

// UI tabs
enum class Tab {
    Seasons,
    Races,
    Sessions,
    Data
};

// Central state for the entire app
class AppState {
public:
    Tab current_tab = Tab::Seasons;
    SessionContext context;
    Cache cache;

    std::vector<int> available_years = {2026, 2025, 2024, 2023};
    int selected_year_index = 0;

    std::vector<data::Meeting> meetings;
    int selected_meeting_index = 0;

    std::vector<data::Session> sessions;
    int selected_session_index = 0;

    std::vector<data::Driver> drivers;
    int selected_driver_index = 0;

    std::vector<data::Lap> laps;
    std::vector<data::Stint> stints;
    std::vector<data::PitStop> pit_stops;
    std::vector<data::RaceControl> race_control;

    bool is_loading = false;
    std::string error_message;

    int get_selected_year() const {
        if (selected_year_index >= 0 && selected_year_index < static_cast<int>(available_years.size())) {
            return available_years[selected_year_index];
        }
        return 0;
    }

    const data::Meeting* get_selected_meeting() const {
        if (selected_meeting_index >= 0 && selected_meeting_index < static_cast<int>(meetings.size())) {
            return &meetings[selected_meeting_index];
        }
        return nullptr;
    }

    const data::Session* get_selected_session() const {
        if (selected_session_index >= 0 && selected_session_index < static_cast<int>(sessions.size())) {
            return &sessions[selected_session_index];
        }
        return nullptr;
    }

    const data::Driver* get_selected_driver() const {
        if (selected_driver_index >= 0 && selected_driver_index < static_cast<int>(drivers.size())) {
            return &drivers[selected_driver_index];
        }
        return nullptr;
    }

    void reset() {
        context.reset();
        meetings.clear();
        sessions.clear();
        drivers.clear();
        laps.clear();
        stints.clear();
        pit_stops.clear();
        race_control.clear();
        selected_meeting_index = 0;
        selected_session_index = 0;
        selected_driver_index = 0;
        error_message.clear();
    }
};

}
