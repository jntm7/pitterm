using F1.Core.Services;
using F1Tui.Configuration;
using F1Tui.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Terminal.Gui;

namespace F1Tui;

public sealed class TerminalApp
{
    private readonly ISeasonService seasonService;
    private readonly IRaceService raceService;
    private readonly ISessionService sessionService;
    private readonly IAppStateStore stateStore;
    private readonly IOptions<AppOptions> options;
    private readonly ILogger<TerminalApp> logger;

    public TerminalApp(
        ISeasonService seasonService,
        IRaceService raceService,
        ISessionService sessionService,
        IAppStateStore stateStore,
        IOptions<AppOptions> options,
        ILogger<TerminalApp> logger)
    {
        this.seasonService = seasonService;
        this.raceService = raceService;
        this.sessionService = sessionService;
        this.stateStore = stateStore;
        this.options = options;
        this.logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Starting PitTerm with API {ApiBaseUrl}, timeout {TimeoutSeconds}s, cache TTL {CacheTtlMinutes}m.",
            options.Value.ApiBaseUrl,
            options.Value.RequestTimeoutSeconds,
            options.Value.CacheTtlMinutes);

        var raceModels = new List<F1.Core.Models.Race>();
        var sessionModels = new List<F1.Core.Models.Session>();

        Application.Init();

        var darkScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.Gray, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black),
            HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Gray),
            Disabled = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black)
        };

        var top = Application.Top;
        top.ColorScheme = darkScheme;

        var window = new Window("PitTerm")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        window.ColorScheme = darkScheme;

        var loadingLabel = new Label("Loading seasons...")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill() - 2,
            Height = 1
        };
        loadingLabel.ColorScheme = darkScheme;

        var statusLine = new Label("Initializing...")
        {
            X = 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill() - 2,
            Height = 1
        };
        statusLine.ColorScheme = darkScheme;

        var shortcutsLine = new Label("[Enter] Select  [Esc] Back  [Q] Quit")
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill() - 2,
            Height = 1
        };
        shortcutsLine.ColorScheme = darkScheme;

        window.Add(loadingLabel, statusLine, shortcutsLine);
        top.Add(window);

        Application.Refresh();

        var seasons = await seasonService.GetSeasonsAsync(cancellationToken);

        stateStore.Update(state => state with
        {
            SelectedSeason = null,
            ActiveScreen = "Seasons",
            StatusMessage = BuildSeasonsStatusMessage(null)
        });

        logger.LogInformation(
            "Initial app state: screen={Screen}, season={Season}",
            stateStore.Current.ActiveScreen,
            stateStore.Current.SelectedSeason);

        var seasonNames = seasons.Select(season => season.Year.ToString()).ToList();

        window.Remove(loadingLabel);

        var title = new Label("F1 Seasons")
        {
            X = 1,
            Y = 1
        };

        var seasonListView = new ListView(seasonNames)
        {
            X = 1,
            Y = Pos.Bottom(title) + 1,
            Width = 30,
            Height = Dim.Fill() - 3
        };
        seasonListView.ColorScheme = darkScheme;

        var raceRows = new List<string>();
        var sessionRows = new List<string>();

        var raceListView = new ListView(raceRows)
        {
            X = 1,
            Y = Pos.Bottom(title) + 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 3,
            Visible = false
        };
        raceListView.ColorScheme = darkScheme;

        var sessionListView = new ListView(sessionRows)
        {
            X = 1,
            Y = Pos.Bottom(title) + 1,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 3,
            Visible = false
        };
        sessionListView.ColorScheme = darkScheme;

        statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";

        top.KeyPress += args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }
        };

        window.KeyPress += args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }
        };

        seasonListView.KeyPress += async args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }

            if (args.KeyEvent.Key == Key.Enter)
            {
                args.Handled = true;
                if (seasonListView.SelectedItem < 0 || seasonListView.SelectedItem >= seasons.Count)
                {
                    return;
                }

                var selectedSeason = seasons[seasonListView.SelectedItem].Year;

                statusLine.Text = $"Loading races for {selectedSeason}...";
                Application.Refresh();

                raceModels = (await raceService.GetRacesBySeasonAsync(selectedSeason, cancellationToken)).ToList();
                raceRows = raceModels
                    .OrderBy(race => race.RoundNumber)
                    .Select(race => $"R{race.RoundNumber}  {race.GrandPrixName} ({race.Season})")
                    .ToList();
                raceListView.SetSource(raceRows);
                seasonListView.Visible = false;
                raceListView.Visible = true;
                title.Text = $"Races - {selectedSeason}";

                stateStore.Update(state => state with
                {
                    SelectedSeason = selectedSeason,
                    SelectedRoundNumber = null,
                    SelectedGrandPrixName = null,
                    SelectedSessionName = null,
                    ActiveScreen = "Races",
                    StatusMessage = BuildRaceStatusMessage(selectedSeason, null)
                });

                statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                raceListView.SetFocus();
            }
        };

        raceListView.KeyPress += async args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;

                raceListView.Visible = false;
                sessionListView.Visible = false;
                seasonListView.Visible = true;
                title.Text = "F1 Seasons";

                stateStore.Update(state => state with
                {
                    SelectedRoundNumber = null,
                    SelectedGrandPrixName = null,
                    SelectedSessionName = null,
                    ActiveScreen = "Seasons",
                    StatusMessage = BuildSeasonsStatusMessage(state.SelectedSeason)
                });

                statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                seasonListView.SetFocus();
            }

            if (args.KeyEvent.Key == Key.Enter)
            {
                args.Handled = true;

                if (raceListView.SelectedItem < 0 || raceListView.SelectedItem >= raceRows.Count)
                {
                    return;
                }

                var selectedEntry = raceRows[raceListView.SelectedItem];
                var selectedRace = raceModels[raceListView.SelectedItem];

                statusLine.Text = $"Loading sessions for {selectedRace.GrandPrixName}...";
                Application.Refresh();

                sessionModels = (await sessionService.GetSessionsByRaceAsync(
                    selectedRace.Season,
                    selectedRace.RoundNumber,
                    selectedRace.MeetingKey,
                    cancellationToken)).ToList();

                sessionRows = sessionModels
                    .Select(session => session.SessionName)
                    .ToList();
                sessionListView.SetSource(sessionRows);

                seasonListView.Visible = false;
                raceListView.Visible = false;
                sessionListView.Visible = true;
                title.Text = "Sessions";

                stateStore.Update(state => state with
                {
                    SelectedRoundNumber = selectedRace.RoundNumber,
                    SelectedGrandPrixName = selectedRace.GrandPrixName,
                    SelectedSessionName = null,
                    ActiveScreen = "Sessions",
                    StatusMessage = BuildSessionsStatusMessage(
                        state.SelectedSeason,
                        selectedRace.RoundNumber,
                        selectedRace.GrandPrixName,
                        null)
                });

                statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                sessionListView.SetFocus();
            }
        };

        sessionListView.KeyPress += args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;

                sessionListView.Visible = false;
                raceListView.Visible = true;
                seasonListView.Visible = false;
                title.Text = $"Races - {stateStore.Current.SelectedSeason?.ToString() ?? "n/a"}";

                stateStore.Update(state => state with
                {
                    SelectedRoundNumber = state.SelectedRoundNumber,
                    SelectedGrandPrixName = state.SelectedGrandPrixName,
                    ActiveScreen = "Races",
                    SelectedSessionName = null,
                    StatusMessage = BuildRaceStatusMessage(state.SelectedSeason, state.SelectedGrandPrixName)
                });

                statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                raceListView.SetFocus();
            }
        };

        seasonListView.SelectedItemChanged += args =>
        {
            var selectedSeason = seasons[args.Item].Year;

            stateStore.Update(state => state with
            {
                SelectedSeason = selectedSeason,
                SelectedRoundNumber = null,
                SelectedGrandPrixName = null,
                SelectedSessionName = null,
                ActiveScreen = "Seasons",
                StatusMessage = BuildSeasonsStatusMessage(selectedSeason)
            });

            statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
        };

        raceListView.SelectedItemChanged += args =>
        {
            if (stateStore.Current.SelectedSeason is null)
            {
                return;
            }

            if (args.Item < 0 || args.Item >= raceRows.Count)
            {
                return;
            }

            var selectedEntry = raceRows[args.Item];
            var selectedRace = raceModels[args.Item];

            stateStore.Update(state => state with
            {
                SelectedRoundNumber = selectedRace.RoundNumber,
                SelectedGrandPrixName = selectedRace.GrandPrixName,
                SelectedSessionName = null,
                StatusMessage = BuildRaceStatusMessage(state.SelectedSeason, selectedEntry)
            });

            statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
        };

        sessionListView.SelectedItemChanged += args =>
        {
            if (args.Item < 0 || args.Item >= sessionRows.Count)
            {
                return;
            }

            var selectedSession = sessionRows[args.Item];
            var selectedRace = raceListView.SelectedItem >= 0 && raceListView.SelectedItem < raceModels.Count
                ? raceModels[raceListView.SelectedItem]
                : null;

            stateStore.Update(state => state with
            {
                SelectedRoundNumber = selectedRace?.RoundNumber,
                SelectedGrandPrixName = selectedRace?.GrandPrixName,
                SelectedSessionName = selectedSession,
                StatusMessage = BuildSessionsStatusMessage(
                    state.SelectedSeason,
                    selectedRace?.RoundNumber,
                    selectedRace?.GrandPrixName,
                    selectedSession)
            });

            statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
        };

        window.Add(title, seasonListView, raceListView, sessionListView, statusLine, shortcutsLine);

        Application.Run();
        Application.Shutdown();
    }

    private string BuildSeasonsStatusMessage(int? selectedSeason)
    {
        if (selectedSeason is null)
        {
            return $"Season: none selected | Data: {options.Value.ApiBaseUrl}";
        }

        return $"Season {selectedSeason} selected | Data: {options.Value.ApiBaseUrl}";
    }

    private string BuildRaceStatusMessage(int? selectedSeason, string? selectedRaceEntry)
    {
        var grandPrix = string.IsNullOrWhiteSpace(selectedRaceEntry) ? "select a race" : selectedRaceEntry;
        return $"Season {selectedSeason?.ToString() ?? "none"} | Grand Prix: {grandPrix} | Data: {options.Value.ApiBaseUrl}";
    }

    private string BuildSessionsStatusMessage(
        int? selectedSeason,
        int? selectedRoundNumber,
        string? selectedGrandPrixName,
        string? selectedSession)
    {
        var roundText = selectedRoundNumber?.ToString() ?? "n/a";
        var grandPrix = string.IsNullOrWhiteSpace(selectedGrandPrixName) ? "n/a" : selectedGrandPrixName;
        var sessionName = string.IsNullOrWhiteSpace(selectedSession) ? "select a session" : selectedSession;
        return $"Season {selectedSeason?.ToString() ?? "none"} | Round {roundText} | Grand Prix: {grandPrix} | Session: {sessionName} | Data: {options.Value.ApiBaseUrl}";
    }

    private static bool ShouldQuit(Key key, int keyValue)
    {
        if (keyValue == 'q' || keyValue == 'Q')
        {
            return true;
        }

        return keyValue == 3 || key == (Key.CtrlMask | (Key)'c') || key == (Key.CtrlMask | (Key)'C');
    }
}