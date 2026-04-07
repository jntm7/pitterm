#pragma once

namespace pitterm::state {

// Tracks the drill-down path: year -> meeting -> session -> driver
struct SessionContext {
    int year;
    int meeting_key;
    int session_key;
    int driver_number;

    bool has_year() const { return year > 0; }
    bool has_meeting() const { return meeting_key > 0; }
    bool has_session() const { return session_key > 0; }
    bool has_driver() const { return driver_number > 0; }

    void reset() {
        year = 0;
        meeting_key = 0;
        session_key = 0;
        driver_number = 0;
    }
};

}
