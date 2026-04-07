#pragma once

#include <nlohmann/json.hpp>
#include <string>

namespace pitterm::data {

// Continuous driving period on one tyre compound
struct Stint {
    int stint_number;
    int driver_number;
    int session_key;
    int meeting_key;
    int lap_start;
    int lap_end;
    std::string compound;
    int tyre_age_at_start;
    bool tyre_age_at_start_is_null;
};

inline void from_json(const nlohmann::json& j, Stint& s) {
    j.at("stint_number").get_to(s.stint_number);
    j.at("driver_number").get_to(s.driver_number);
    j.at("session_key").get_to(s.session_key);
    j.at("meeting_key").get_to(s.meeting_key);
    j.at("lap_start").get_to(s.lap_start);
    j.at("lap_end").get_to(s.lap_end);
    j.at("compound").get_to(s.compound);
    
    if (j.contains("tyre_age_at_start") && !j["tyre_age_at_start"].is_null()) {
        s.tyre_age_at_start = j["tyre_age_at_start"].get<int>();
        s.tyre_age_at_start_is_null = false;
    } else {
        s.tyre_age_at_start_is_null = true;
    }
}

}