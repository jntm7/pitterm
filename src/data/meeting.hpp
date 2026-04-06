#pragma once

#include <nlohmann/json.hpp>
#include <string>

namespace pitterm::data {

struct Meeting {
    int meeting_key;
    std::string meeting_name;
    std::string meeting_official_name;
    int year;
    std::string location;
    std::string country_name;
    std::string country_code;
    int circuit_key;
    std::string circuit_short_name;
    std::string circuit_type;
    std::string date_start;
    std::string date_end;
    std::string gmt_offset;
    
    std::string circuit_image;
    std::string circuit_info_url;
    std::string country_flag;
    int country_key;
};

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE_WITH_DEFAULT(Meeting,
    meeting_key, meeting_name, meeting_official_name, year, location,
    country_name, country_code, circuit_key, circuit_short_name,
    circuit_type, date_start, date_end, gmt_offset, circuit_image,
    circuit_info_url, country_flag, country_key);

}