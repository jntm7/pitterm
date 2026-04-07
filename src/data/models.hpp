#pragma once

// Re-exports all models and provides vector aliases
#include "meeting.hpp"
#include "session.hpp"
#include "driver.hpp"
#include "lap.hpp"
#include "stint.hpp"
#include "pit_stop.hpp"
#include "race_control.hpp"

namespace pitterm::data {

using MeetingList = std::vector<Meeting>;
using SessionList = std::vector<Session>;
using DriverList = std::vector<Driver>;
using LapList = std::vector<Lap>;
using StintList = std::vector<Stint>;
using PitStopList = std::vector<PitStop>;
using RaceControlList = std::vector<RaceControl>;

}