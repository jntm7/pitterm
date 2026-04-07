#pragma once

#include <nlohmann/json.hpp>
#include <string>

namespace pitterm::data {

// F1 driver info (number, name, team)
struct Driver {
    int driver_number;
    std::string full_name;
    std::string first_name;
    std::string last_name;
    std::string name_acronym;
    std::string broadcast_name;
    std::string team_name;
    std::string team_colour;
    std::string headshot_url;
    int meeting_key;
    int session_key;
};

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(Driver,
    driver_number, full_name, first_name, last_name, name_acronym,
    broadcast_name, team_name, team_colour, headshot_url,
    meeting_key, session_key);

}