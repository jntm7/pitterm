#pragma once

#include <nlohmann/json.hpp>
#include <string>

namespace pitterm::data {

struct Session {
    int session_key;
    int meeting_key;
    std::string session_name;
    std::string session_type;
    std::string date_start;
    std::string date_end;
    std::string gmt_offset;
    int year;
    std::string location;
    std::string country_name;
    std::string country_code;
    int circuit_key;
    std::string circuit_short_name;
    int country_key;
};

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(Session,
    session_key, meeting_key, session_name, session_type,
    date_start, date_end, gmt_offset, year, location,
    country_name, country_code, circuit_key, circuit_short_name, country_key);

}