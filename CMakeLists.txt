cmake_minimum_required(VERSION 3.20)
project(pitterm VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

include(FetchContent)

FetchContent_Declare(
    ftxui
    GIT_REPOSITORY https://github.com/ArthurSonzogni/FTXUI.git
    GIT_TAG v6.1.9
)

FetchContent_Declare(
    json
    GIT_REPOSITORY https://github.com/nlohmann/json.git
    GIT_TAG v3.12.0
)

FetchContent_Declare(
    httplib
    GIT_REPOSITORY https://github.com/yhirose/cpp-httplib.git
    GIT_TAG v0.20.1
)

FetchContent_MakeAvailable(ftxui json httplib)

add_executable(pitterm
    src/main.cpp
)

target_link_libraries(pitterm PRIVATE
    ftxui::screen
    ftxui::dom
    ftxui::component
    nlohmann_json::nlohmann_json
    httplib::httplib
)

if(MSVC)
    target_compile_options(pitterm PRIVATE /W4)
else()
    target_compile_options(pitterm PRIVATE -Wall -Wextra -Wpedantic)
endif()