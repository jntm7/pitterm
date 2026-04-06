#include <ftxui/component/component.hpp>
#include <ftxui/component/screen_interactive.hpp>
#include <ftxui/dom/elements.hpp>

int main() {
    using namespace ftxui;

    auto screen = ScreenInteractive::TerminalOutput();
    auto component = Renderer([] {
        return vbox({
            text("PitTerm - F1 Historical Data Viewer") | bold | hcenter,
            separator(),
            text("Press Ctrl+C to exit") | dim | hcenter,
        });
    });
    screen.Loop(component);
    return 0;
}