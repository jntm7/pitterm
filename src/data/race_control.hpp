#pragma once

#include <nlohmann/json.hpp>
#include <optional>
#include <string>

namespace pitterm::data {

// Flags, safety car, incidents, session status
struct RaceControl {
    int session_key;
    int meeting_key;
    std::string date;
    std::string category;
    std::string message;
    std::optional<int> driver_number;
    std::optional<std::string> flag;
    std::optional<std::string> scope;
    std::optional<int> sector;
    std::optional<int> lap_number;
    std::optional<int> qualifying_phase;
};

inline void from_json(const nlohmann::json& j, RaceControl& r) {
    j.at("session_key").get_to(r.session_key);
    j.at("meeting_key").get_to(r.meeting_key);
    j.at("date").get_to(r.date);
    j.at("category").get_to(r.category);
    j.at("message").get_to(r.message);
    
    if (j.contains("driver_number") && !j["driver_number"].is_null()) {
        r.driver_number = j["driver_number"].get<int>();
    }
    if (j.contains("flag") && !j["flag"].is_null()) {
        r.flag = j["flag"].get<std::string>();
    }
    if (j.contains("scope") && !j["scope"].is_null()) {
        r.scope = j["scope"].get<std::string>();
    }
    if (j.contains("sector") && !j["sector"].is_null()) {
        r.sector = j["sector"].get<int>();
    }
    if (j.contains("lap_number") && !j["lap_number"].is_null()) {
        r.lap_number = j["lap_number"].get<int>();
    }
    if (j.contains("qualifying_phase") && !j["qualifying_phase"].is_null()) {
        r.qualifying_phase = j["qualifying_phase"].get<int>();
    }
}

}