#pragma once

#include <nlohmann/json.hpp>
#include <optional>
#include <string>
#include <vector>

namespace pitterm::data {

// Single lap with timing and sector data
struct Lap {
    int lap_number;
    int driver_number;
    int session_key;
    int meeting_key;
    std::string date_start;
    double lap_duration;
    std::optional<double> duration_sector_1;
    std::optional<double> duration_sector_2;
    std::optional<double> duration_sector_3;
    std::optional<int> i1_speed;
    std::optional<int> i2_speed;
    std::optional<int> st_speed;
    bool is_pit_out_lap;
    std::optional<std::vector<int>> segments_sector_1;
    std::optional<std::vector<int>> segments_sector_2;
    std::optional<std::vector<int>> segments_sector_3;
};

inline void from_json(const nlohmann::json& j, Lap& l) {
    j.at("lap_number").get_to(l.lap_number);
    j.at("driver_number").get_to(l.driver_number);
    j.at("session_key").get_to(l.session_key);
    j.at("meeting_key").get_to(l.meeting_key);
    j.at("date_start").get_to(l.date_start);
    j.at("lap_duration").get_to(l.lap_duration);
    
    if (j.contains("duration_sector_1") && !j["duration_sector_1"].is_null()) {
        l.duration_sector_1 = j["duration_sector_1"].get<double>();
    }
    if (j.contains("duration_sector_2") && !j["duration_sector_2"].is_null()) {
        l.duration_sector_2 = j["duration_sector_2"].get<double>();
    }
    if (j.contains("duration_sector_3") && !j["duration_sector_3"].is_null()) {
        l.duration_sector_3 = j["duration_sector_3"].get<double>();
    }
    if (j.contains("i1_speed") && !j["i1_speed"].is_null()) {
        l.i1_speed = j["i1_speed"].get<int>();
    }
    if (j.contains("i2_speed") && !j["i2_speed"].is_null()) {
        l.i2_speed = j["i2_speed"].get<int>();
    }
    if (j.contains("st_speed") && !j["st_speed"].is_null()) {
        l.st_speed = j["st_speed"].get<int>();
    }
    if (j.contains("is_pit_out_lap")) {
        l.is_pit_out_lap = j["is_pit_out_lap"].get<bool>();
    }
    if (j.contains("segments_sector_1") && !j["segments_sector_1"].is_null()) {
        l.segments_sector_1 = j["segments_sector_1"].get<std::vector<int>>();
    }
    if (j.contains("segments_sector_2") && !j["segments_sector_2"].is_null()) {
        l.segments_sector_2 = j["segments_sector_2"].get<std::vector<int>>();
    }
    if (j.contains("segments_sector_3") && !j["segments_sector_3"].is_null()) {
        l.segments_sector_3 = j["segments_sector_3"].get<std::vector<int>>();
    }
}

}