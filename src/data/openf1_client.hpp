#pragma once

#include <string>
#include <vector>

namespace pitterm::data {

struct Meeting;
struct Session;
struct Driver;
struct Lap;
struct Stint;
struct PitStop;
struct RaceControl;

}

namespace pitterm::api {

constexpr const char* BASE_URL = "https://api.openf1.org/v1";

// HTTP client for OpenF1 API endpoints
class OpenF1Client {
public:
    std::vector<data::Meeting> get_meetings(int year);
    std::vector<data::Session> get_sessions(int meeting_key);
    std::vector<data::Session> get_sessions_by_year(int year);
    std::vector<data::Driver> get_drivers(int session_key);
    std::vector<data::Lap> get_laps(int session_key, int driver_number);
    std::vector<data::Stint> get_stints(int session_key);
    std::vector<data::PitStop> get_pit_stops(int session_key);
    std::vector<data::RaceControl> get_race_control(int session_key);
};

}