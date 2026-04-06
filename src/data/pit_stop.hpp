#pragma once

#include <nlohmann/json.hpp>
#include <optional>
#include <string>

namespace pitterm::data {

struct PitStop {
    int driver_number;
    int session_key;
    int meeting_key;
    int lap_number;
    std::string date;
    double lane_duration;
    std::optional<double> stop_duration;
};

inline void from_json(const nlohmann::json& j, PitStop& p) {
    j.at("driver_number").get_to(p.driver_number);
    j.at("session_key").get_to(p.session_key);
    j.at("meeting_key").get_to(p.meeting_key);
    j.at("lap_number").get_to(p.lap_number);
    j.at("date").get_to(p.date);
    j.at("lane_duration").get_to(p.lane_duration);
    
    if (j.contains("stop_duration") && !j["stop_duration"].is_null()) {
        p.stop_duration = j["stop_duration"].get<double>();
    }
}

}