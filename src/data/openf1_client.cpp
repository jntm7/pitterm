#include "openf1_client.hpp"

#include <httplib.h>
#include <nlohmann/json.hpp>

#include <format>
#include <stdexcept>

#include "models.hpp"

namespace pitterm::api {

namespace {

// Helper: builds URL with query params
std::string build_url(const std::string& endpoint,
                      const std::vector<std::pair<std::string, std::string>>& params) {
    std::string url = std::format("{}/{}", BASE_URL, endpoint);
    if (!params.empty()) {
        url += "?";
        bool first = true;
        for (const auto& [key, value] : params) {
            if (!first) url += "&";
            url += key + "=" + value;
            first = false;
        }
    }
    return url;
}

// Helper: fetches and parses JSON from API
nlohmann::json fetch_json(const std::string& endpoint,
                          const std::vector<std::pair<std::string, std::string>>& params = {}) {
    httplib::Client cli(BASE_URL);
    auto url = build_url(endpoint, params);
    
    auto response = cli.Get(url.c_str());
    
    if (!response || response->status != 200) {
        throw std::runtime_error(std::format("API request failed: {}", url));
    }
    
    return nlohmann::json::parse(response->body);
}

}

std::vector<data::Meeting> OpenF1Client::get_meetings(int year) {
    auto json = fetch_json("meetings", {{"year", std::to_string(year)}});
    return json.get<std::vector<data::Meeting>>();
}

std::vector<data::Session> OpenF1Client::get_sessions(int meeting_key) {
    auto json = fetch_json("sessions", {{"meeting_key", std::to_string(meeting_key)}});
    return json.get<std::vector<data::Session>>();
}

std::vector<data::Session> OpenF1Client::get_sessions_by_year(int year) {
    auto json = fetch_json("sessions", {{"year", std::to_string(year)}});
    return json.get<std::vector<data::Session>>();
}

std::vector<data::Driver> OpenF1Client::get_drivers(int session_key) {
    auto json = fetch_json("drivers", {{"session_key", std::to_string(session_key)}});
    return json.get<std::vector<data::Driver>>();
}

std::vector<data::Lap> OpenF1Client::get_laps(int session_key, int driver_number) {
    auto json = fetch_json("laps", {
        {"session_key", std::to_string(session_key)},
        {"driver_number", std::to_string(driver_number)}
    });
    
    std::vector<data::Lap> laps;
    for (const auto& j : json) {
        laps.push_back(j.get<data::Lap>());
    }
    return laps;
}

std::vector<data::Stint> OpenF1Client::get_stints(int session_key) {
    auto json = fetch_json("stints", {{"session_key", std::to_string(session_key)}});
    return json.get<std::vector<data::Stint>>();
}

std::vector<data::PitStop> OpenF1Client::get_pit_stops(int session_key) {
    auto json = fetch_json("pit", {{"session_key", std::to_string(session_key)}});
    
    std::vector<data::PitStop> pitstops;
    for (const auto& j : json) {
        pitstops.push_back(j.get<data::PitStop>());
    }
    return pitstops;
}

std::vector<data::RaceControl> OpenF1Client::get_race_control(int session_key) {
    auto json = fetch_json("race_control", {{"session_key", std::to_string(session_key)}});
    
    std::vector<data::RaceControl> messages;
    for (const auto& j : json) {
        messages.push_back(j.get<data::RaceControl>());
    }
    return messages;
}

}