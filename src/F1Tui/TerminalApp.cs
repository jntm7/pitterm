using F1.Core.Services;
using F1Tui.Configuration;
using F1Tui.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Terminal.Gui;

namespace F1Tui;

public sealed class TerminalApp
{
    private enum RaceHubTab
    {
        Sessions = 0,
        DriverStandings = 1,
        ConstructorStandings = 2
    }

    private enum SessionDetailTab
    {
        SessionInfo = 0,
        RaceResults = 1
    }

    private readonly ISeasonService seasonService;
    private readonly IRaceService raceService;
    private readonly ISessionService sessionService;
    private readonly IRaceResultsService raceResultsService;
    private readonly IDriverStandingsService driverStandingsService;
    private readonly IConstructorStandingsService constructorStandingsService;
    private readonly IAppStateStore stateStore;
    private readonly IOptions<AppOptions> options;
    private readonly ILogger<TerminalApp> logger;

    public TerminalApp(
        ISeasonService seasonService,
        IRaceService raceService,
        ISessionService sessionService,
        IRaceResultsService raceResultsService,
        IDriverStandingsService driverStandingsService,
        IConstructorStandingsService constructorStandingsService,
        IAppStateStore stateStore,
        IOptions<AppOptions> options,
        ILogger<TerminalApp> logger)
    {
        this.seasonService = seasonService;
        this.raceService = raceService;
        this.sessionService = sessionService;
        this.raceResultsService = raceResultsService;
        this.driverStandingsService = driverStandingsService;
        this.constructorStandingsService = constructorStandingsService;
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

        var statusLine = new Label("Initializing...")
        {
            X = 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill() - 2,
            Height = 1
        };
        statusLine.ColorScheme = darkScheme;

        var shortcutsLine = new Label(BuildDefaultShortcutsText())
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill() - 2,
            Height = 1
        };
        shortcutsLine.ColorScheme = darkScheme;

        top.Add(window);

        var currentYear = DateTime.UtcNow.Year;
        var seasons = Enumerable.Range(2023, Math.Max(currentYear - 2023 + 1, 1))
            .OrderByDescending(year => year)
            .Select(year => new F1.Core.Models.Season(year))
            .ToList();

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

        _ = Task.Run(async () =>
        {
            try
            {
                var fetchedSeasons = await seasonService.GetSeasonsAsync(cancellationToken);
                if (fetchedSeasons.Count == 0)
                {
                    return;
                }

                var fetchedNames = fetchedSeasons.Select(season => season.Year.ToString()).ToList();
                Application.MainLoop?.Invoke(() =>
                {
                    var currentNames = seasons.Select(season => season.Year.ToString()).ToList();
                    if (fetchedNames.SequenceEqual(currentNames))
                    {
                        return;
                    }

                    seasons = fetchedSeasons.ToList();
                    seasonListView.SetSource(fetchedNames);
                    statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                });
            }
            catch
            {
            }
        }, cancellationToken);

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
            Y = Pos.Bottom(title) + 2,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 4,
            Visible = false
        };
        sessionListView.ColorScheme = darkScheme;

        var raceHubTabsLabel = new Label(string.Empty)
        {
            X = 1,
            Y = Pos.Bottom(title) + 1,
            Width = Dim.Fill() - 2,
            Height = 1,
            Visible = false
        };
        raceHubTabsLabel.ColorScheme = darkScheme;

        var driverStandingsView = new TextView
        {
            X = 1,
            Y = Pos.Bottom(title) + 2,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 4,
            ReadOnly = true,
            Visible = false,
            WordWrap = false,
            Text = string.Empty
        };
        driverStandingsView.ColorScheme = darkScheme;

        var constructorStandingsView = new TextView
        {
            X = 1,
            Y = Pos.Bottom(title) + 2,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 4,
            ReadOnly = true,
            Visible = false,
            WordWrap = false,
            Text = string.Empty
        };
        constructorStandingsView.ColorScheme = darkScheme;

        var sessionDetailView = new TextView
        {
            X = 1,
            Y = Pos.Bottom(title) + 2,
            Width = Dim.Fill() - 2,
            Height = Dim.Fill() - 4,
            ReadOnly = true,
            Visible = false,
            WordWrap = false,
            Text = string.Empty
        };
        sessionDetailView.ColorScheme = darkScheme;

        var sessionDetailTabsLabel = new Label(string.Empty)
        {
            X = 1,
            Y = Pos.Bottom(title) + 1,
            Width = Dim.Fill() - 2,
            Height = 1,
            Visible = false
        };
        sessionDetailTabsLabel.ColorScheme = darkScheme;

        var selectedRaceHubTab = RaceHubTab.Sessions;
        var selectedDetailTab = SessionDetailTab.SessionInfo;
        F1.Core.Models.Session? detailSession = null;
        F1.Core.Models.Race? detailRace = null;
        var driverStandingsLines = new List<string>();
        var driverStandingsOffset = 0;

        void RenderDriverStandingsViewport()
        {
            if (driverStandingsLines.Count == 0)
            {
                driverStandingsView.Text = string.Empty;
                return;
            }

            var viewportHeight = Math.Max(driverStandingsView.Bounds.Height, 5);
            var maxOffset = Math.Max(driverStandingsLines.Count - viewportHeight, 0);
            driverStandingsOffset = Math.Clamp(driverStandingsOffset, 0, maxOffset);

            var visibleLines = driverStandingsLines
                .Skip(driverStandingsOffset)
                .Take(viewportHeight)
                .ToList();

            driverStandingsView.Text = string.Join(Environment.NewLine, visibleLines);
        }

        async Task RefreshRaceHubTabAsync()
        {
            raceHubTabsLabel.Text = BuildRaceHubTabsHeader(selectedRaceHubTab);
            raceHubTabsLabel.Visible = true;

            sessionListView.Visible = selectedRaceHubTab == RaceHubTab.Sessions;
            driverStandingsView.Visible = selectedRaceHubTab == RaceHubTab.DriverStandings;
            constructorStandingsView.Visible = selectedRaceHubTab == RaceHubTab.ConstructorStandings;

            if (selectedRaceHubTab == RaceHubTab.DriverStandings)
            {
                driverStandingsView.Text = "Loading driver standings...";
                try
                {
                    var standings = await driverStandingsService.GetDriverStandingsAsync(
                        stateStore.Current.SelectedSeason ?? detailSession?.Season ?? 2025,
                        detailRace?.MeetingKey,
                        cancellationToken);

                    driverStandingsLines = BuildDriverStandingsTableText(
                        stateStore.Current.SelectedSeason,
                        detailRace,
                        standings)
                        .Replace("\r", string.Empty)
                        .Split('\n')
                        .ToList();
                    driverStandingsOffset = 0;
                    RenderDriverStandingsViewport();
                }
                catch
                {
                    driverStandingsLines = BuildDriverStandingsPlaceholder(stateStore.Current.SelectedSeason, detailRace)
                        .Replace("\r", string.Empty)
                        .Split('\n')
                        .ToList();
                    driverStandingsOffset = 0;
                    RenderDriverStandingsViewport();
                }
            }

            if (selectedRaceHubTab == RaceHubTab.ConstructorStandings)
            {
                constructorStandingsView.Text = "Loading constructor standings...";
                try
                {
                    var standings = await constructorStandingsService.GetConstructorStandingsAsync(
                        stateStore.Current.SelectedSeason ?? detailSession?.Season ?? 2025,
                        detailRace?.MeetingKey,
                        cancellationToken);

                    constructorStandingsView.Text = BuildConstructorStandingsTableText(
                        stateStore.Current.SelectedSeason,
                        detailRace,
                        standings);
                }
                catch
                {
                    constructorStandingsView.Text = BuildConstructorStandingsPlaceholder(stateStore.Current.SelectedSeason, detailRace);
                }
            }

            if (selectedRaceHubTab == RaceHubTab.Sessions)
            {
                sessionListView.SetFocus();
            }
            else if (selectedRaceHubTab == RaceHubTab.DriverStandings)
            {
                driverStandingsView.SetFocus();
            }
            else
            {
                constructorStandingsView.SetFocus();
            }
        }

        async Task NavigateRaceHubTabAsync(Key key, int keyValue)
        {
            if (key == Key.CursorLeft)
            {
                selectedRaceHubTab = selectedRaceHubTab == RaceHubTab.Sessions
                    ? RaceHubTab.ConstructorStandings
                    : selectedRaceHubTab - 1;
                await RefreshRaceHubTabAsync();
                return;
            }

            if (key == Key.CursorRight)
            {
                selectedRaceHubTab = selectedRaceHubTab == RaceHubTab.ConstructorStandings
                    ? RaceHubTab.Sessions
                    : selectedRaceHubTab + 1;
                await RefreshRaceHubTabAsync();
                return;
            }

            if (keyValue >= '1' && keyValue <= '3')
            {
                selectedRaceHubTab = (RaceHubTab)(keyValue - '1');
                await RefreshRaceHubTabAsync();
            }
        }

        async Task RefreshDetailTabAsync()
        {
            if (detailSession is null)
            {
                return;
            }

            sessionDetailTabsLabel.Text = BuildSessionDetailTabsHeader(selectedDetailTab);

            if (selectedDetailTab == SessionDetailTab.RaceResults)
            {
                sessionDetailView.Text = "Loading race results...";
                try
                {
                    var results = await raceResultsService.GetRaceResultsAsync(
                        stateStore.Current.SelectedSeason ?? detailSession.Season,
                        detailSession.MeetingKey,
                        detailSession.SessionKey,
                        cancellationToken);

                    sessionDetailView.Text = BuildRaceResultsTableText(
                        stateStore.Current.SelectedSeason,
                        detailRace,
                        results);
                }
                catch
                {
                    sessionDetailView.Text = BuildRaceResultsPlaceholder(stateStore.Current.SelectedSeason, detailRace, detailSession);
                }

                return;
            }

            sessionDetailView.Text = BuildSessionDetailTextForTab(
                selectedDetailTab,
                stateStore.Current.SelectedSeason,
                detailRace,
                detailSession);
        }

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
                    .Select(race => $"R{race.RoundNumber}  {race.GrandPrixName}")
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
                raceHubTabsLabel.Visible = false;
                driverStandingsView.Visible = false;
                constructorStandingsView.Visible = false;
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
                sessionDetailView.Text = string.Empty;
                sessionDetailTabsLabel.Text = string.Empty;
                detailSession = null;
                detailRace = selectedRace;

                seasonListView.Visible = false;
                raceListView.Visible = false;
                sessionListView.Visible = true;
                sessionDetailView.Visible = false;
                sessionDetailTabsLabel.Visible = false;
                raceHubTabsLabel.Visible = true;
                title.Text = "Sessions";
                shortcutsLine.Text = BuildRaceHubShortcutsText();

                selectedRaceHubTab = RaceHubTab.Sessions;
                await RefreshRaceHubTabAsync();

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

        sessionListView.KeyPress += async args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }

            if (args.KeyEvent.Key == Key.CursorLeft || args.KeyEvent.Key == Key.CursorRight ||
                (args.KeyEvent.KeyValue >= '1' && args.KeyEvent.KeyValue <= '3'))
            {
                args.Handled = true;
                await NavigateRaceHubTabAsync(args.KeyEvent.Key, args.KeyEvent.KeyValue);
            }

            if (args.KeyEvent.Key == Key.Enter)
            {
                args.Handled = true;

                if (sessionListView.SelectedItem < 0 || sessionListView.SelectedItem >= sessionModels.Count)
                {
                    return;
                }

                var selectedSessionModel = sessionModels[sessionListView.SelectedItem];
                var selectedRaceModel = raceListView.SelectedItem >= 0 && raceListView.SelectedItem < raceModels.Count
                    ? raceModels[raceListView.SelectedItem]
                    : null;

                detailSession = selectedSessionModel;
                detailRace = selectedRaceModel;
                selectedDetailTab = SessionDetailTab.SessionInfo;
                await RefreshDetailTabAsync();

                sessionListView.Visible = false;
                sessionDetailView.Visible = true;
                sessionDetailTabsLabel.Visible = true;
                title.Text = "Session Detail";
                shortcutsLine.Text = BuildSessionDetailShortcutsText();

                stateStore.Update(state => state with
                {
                    SelectedSessionName = selectedSessionModel.SessionName,
                    ActiveScreen = "SessionDetail",
                    StatusMessage = BuildSessionsStatusMessage(
                        state.SelectedSeason,
                        selectedSessionModel.RoundNumber,
                        state.SelectedGrandPrixName,
                        selectedSessionModel.SessionName)
                });

                statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                sessionDetailView.SetFocus();
            }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;

                sessionListView.Visible = false;
                raceListView.Visible = true;
                seasonListView.Visible = false;
                sessionDetailView.Visible = false;
                sessionDetailTabsLabel.Visible = false;
                raceHubTabsLabel.Visible = false;
                driverStandingsView.Visible = false;
                constructorStandingsView.Visible = false;
                title.Text = $"Races - {stateStore.Current.SelectedSeason?.ToString() ?? "n/a"}";
                shortcutsLine.Text = BuildDefaultShortcutsText();

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

        driverStandingsView.KeyPress += async args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }

            if (args.KeyEvent.Key == Key.CursorDown)
            {
                args.Handled = true;
                driverStandingsOffset += 1;
                RenderDriverStandingsViewport();
            }

            if (args.KeyEvent.Key == Key.CursorUp)
            {
                args.Handled = true;
                driverStandingsOffset -= 1;
                RenderDriverStandingsViewport();
            }

            if (args.KeyEvent.Key == Key.CursorLeft || args.KeyEvent.Key == Key.CursorRight ||
                (args.KeyEvent.KeyValue >= '1' && args.KeyEvent.KeyValue <= '3'))
            {
                args.Handled = true;
                await NavigateRaceHubTabAsync(args.KeyEvent.Key, args.KeyEvent.KeyValue);
            }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;

                raceHubTabsLabel.Visible = false;
                driverStandingsView.Visible = false;
                constructorStandingsView.Visible = false;
                sessionListView.Visible = false;
                raceListView.Visible = true;
                seasonListView.Visible = false;
                title.Text = $"Races - {stateStore.Current.SelectedSeason?.ToString() ?? "n/a"}";
                shortcutsLine.Text = BuildDefaultShortcutsText();

                stateStore.Update(state => state with
                {
                    ActiveScreen = "Races",
                    SelectedSessionName = null,
                    StatusMessage = BuildRaceStatusMessage(state.SelectedSeason, state.SelectedGrandPrixName)
                });

                statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                raceListView.SetFocus();
            }
        };

        constructorStandingsView.KeyPress += async args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }

            if (args.KeyEvent.Key == Key.CursorLeft || args.KeyEvent.Key == Key.CursorRight ||
                (args.KeyEvent.KeyValue >= '1' && args.KeyEvent.KeyValue <= '3'))
            {
                args.Handled = true;
                await NavigateRaceHubTabAsync(args.KeyEvent.Key, args.KeyEvent.KeyValue);
            }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;

                raceHubTabsLabel.Visible = false;
                driverStandingsView.Visible = false;
                constructorStandingsView.Visible = false;
                sessionListView.Visible = false;
                raceListView.Visible = true;
                seasonListView.Visible = false;
                title.Text = $"Races - {stateStore.Current.SelectedSeason?.ToString() ?? "n/a"}";
                shortcutsLine.Text = BuildDefaultShortcutsText();

                stateStore.Update(state => state with
                {
                    ActiveScreen = "Races",
                    SelectedSessionName = null,
                    StatusMessage = BuildRaceStatusMessage(state.SelectedSeason, state.SelectedGrandPrixName)
                });

                statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                raceListView.SetFocus();
            }
        };

        sessionDetailView.KeyPress += async args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }

            if (args.KeyEvent.Key == Key.CursorLeft)
            {
                args.Handled = true;
                selectedDetailTab = selectedDetailTab == SessionDetailTab.SessionInfo
                    ? SessionDetailTab.RaceResults
                    : selectedDetailTab - 1;
                await RefreshDetailTabAsync();
            }

            if (args.KeyEvent.Key == Key.CursorRight)
            {
                args.Handled = true;
                selectedDetailTab = selectedDetailTab == SessionDetailTab.RaceResults
                    ? SessionDetailTab.SessionInfo
                    : selectedDetailTab + 1;
                await RefreshDetailTabAsync();
            }

            if (args.KeyEvent.KeyValue >= '1' && args.KeyEvent.KeyValue <= '2')
            {
                args.Handled = true;
                selectedDetailTab = (SessionDetailTab)(args.KeyEvent.KeyValue - '1');
                await RefreshDetailTabAsync();
            }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;

                sessionDetailView.Visible = false;
                sessionDetailTabsLabel.Visible = false;
                sessionListView.Visible = true;
                raceListView.Visible = false;
                seasonListView.Visible = false;
                raceHubTabsLabel.Visible = true;
                title.Text = "Sessions";
                shortcutsLine.Text = BuildRaceHubShortcutsText();

                stateStore.Update(state => state with
                {
                    ActiveScreen = "Sessions",
                    StatusMessage = BuildSessionsStatusMessage(
                        state.SelectedSeason,
                        state.SelectedRoundNumber,
                        state.SelectedGrandPrixName,
                        state.SelectedSessionName)
                });

                statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                sessionListView.SetFocus();
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

        window.Add(
            title,
            seasonListView,
            raceListView,
            raceHubTabsLabel,
            sessionListView,
            driverStandingsView,
            constructorStandingsView,
            sessionDetailTabsLabel,
            sessionDetailView,
            statusLine,
            shortcutsLine);

        Application.Run();
        Application.Shutdown();
    }

    private string BuildSeasonsStatusMessage(int? selectedSeason)
    {
        if (selectedSeason is null)
        {
            return $"Season: N/A | Data: {options.Value.ApiBaseUrl}";
        }

        return $"Season {selectedSeason} | Data: {options.Value.ApiBaseUrl}";
    }

    private string BuildRaceStatusMessage(int? selectedSeason, string? selectedRaceEntry)
    {
        var grandPrix = string.IsNullOrWhiteSpace(selectedRaceEntry) ? "Select Race" : selectedRaceEntry;
        return $"Season {selectedSeason?.ToString() ?? "none"} | {grandPrix} | Data: {options.Value.ApiBaseUrl}";
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
        return $"Season {selectedSeason?.ToString() ?? "none"} | R{roundText} | {grandPrix} | {sessionName}";
    }

    private static string BuildSessionDetailTextForTab(
        SessionDetailTab tab,
        int? selectedSeason,
        F1.Core.Models.Race? selectedRace,
        F1.Core.Models.Session selectedSession)
    {
        if (tab == SessionDetailTab.RaceResults)
        {
            return BuildRaceResultsPlaceholder(selectedSeason, selectedRace, selectedSession);
        }

        var seasonText = selectedSeason?.ToString() ?? "n/a";
        var roundText = selectedSession.RoundNumber.ToString();
        var grandPrixText = selectedRace?.GrandPrixName ?? "n/a";
        var startText = selectedSession.StartTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz") ?? "n/a";
        var endText = selectedSession.EndTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz") ?? "n/a";
        var meetingKeyText = selectedSession.MeetingKey?.ToString() ?? "n/a";
        var sessionKeyText = selectedSession.SessionKey?.ToString() ?? "n/a";

        return string.Join(
            Environment.NewLine,
            "Session Information",
            string.Empty,
            $"Season: {seasonText}",
            $"Round: {roundText}",
            $"Grand Prix: {grandPrixText}",
            $"Session: {selectedSession.SessionName}",
            string.Empty,
            $"Start: {startText}",
            $"End: {endText}",
            string.Empty,
            $"Meeting Key: {meetingKeyText}",
            $"Session Key: {sessionKeyText}");
    }

    private static string BuildSessionDetailTabsHeader(SessionDetailTab selectedTab)
    {
        return string.Join(
            "  ",
            BuildTabLabel(SessionDetailTab.SessionInfo, selectedTab, "1) Session Info"),
            BuildTabLabel(SessionDetailTab.RaceResults, selectedTab, "2) Results"));
    }

    private static string BuildRaceHubTabsHeader(RaceHubTab selectedTab)
    {
        return string.Join(
            "  ",
            BuildRaceHubTabLabel(RaceHubTab.Sessions, selectedTab, "1) Sessions"),
            BuildRaceHubTabLabel(RaceHubTab.DriverStandings, selectedTab, "2) Drivers"),
            BuildRaceHubTabLabel(RaceHubTab.ConstructorStandings, selectedTab, "3) Constructors"));
    }

    private static string BuildRaceHubTabLabel(RaceHubTab tab, RaceHubTab selectedTab, string label)
    {
        return tab == selectedTab ? $"[{label}]" : label;
    }

    private static string BuildTabLabel(SessionDetailTab tab, SessionDetailTab selectedTab, string label)
    {
        return tab == selectedTab ? $"[{label}]" : label;
    }

    private static string BuildRaceResultsPlaceholder(
        int? selectedSeason,
        F1.Core.Models.Race? selectedRace,
        F1.Core.Models.Session selectedSession)
    {
        return string.Join(
            Environment.NewLine,
            "Race Results",
            string.Empty,
            $"Season: {selectedSeason?.ToString() ?? "n/a"}",
            $"Round: {selectedSession.RoundNumber}",
            $"Grand Prix: {selectedRace?.GrandPrixName ?? "n/a"}",
            string.Empty,
            "Source endpoint:",
            "v1/position + v1/drivers",
            string.Empty,
            "Falling back due to unavailable or incomplete data.");
    }

    private static string BuildRaceResultsTableText(
        int? selectedSeason,
        F1.Core.Models.Race? selectedRace,
        IReadOnlyList<F1.Core.Models.RaceResult> results)
    {
        var lines = new List<string>
        {
            "Race Results",
            string.Empty,
            $"Season: {selectedSeason?.ToString() ?? "n/a"}",
            $"Grand Prix: {selectedRace?.GrandPrixName ?? "n/a"}",
            string.Empty,
            "Pos  Driver                    Team",
            "---  ------------------------  -----------------------"
        };

        foreach (var result in results)
        {
            lines.Add($"{result.Position,3}  {Truncate(result.DriverName, 24),-24}  {Truncate(result.TeamName, 23),-23}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildDriverStandingsPlaceholder(int? selectedSeason, F1.Core.Models.Race? selectedRace)
    {
        return string.Join(
            Environment.NewLine,
            "Driver Championship Standings",
            string.Empty,
            $"Season: {selectedSeason?.ToString() ?? "n/a"}",
            $"After: {selectedRace?.GrandPrixName ?? "selected race"}",
            string.Empty,
            "Source endpoint:",
            "v1/championship_drivers",
            string.Empty,
            "Standings table integration is next up.");
    }

    private static string BuildConstructorStandingsPlaceholder(int? selectedSeason, F1.Core.Models.Race? selectedRace)
    {
        return string.Join(
            Environment.NewLine,
            "Constructor Championship Standings",
            string.Empty,
            $"Season: {selectedSeason?.ToString() ?? "n/a"}",
            $"After: {selectedRace?.GrandPrixName ?? "selected race"}",
            string.Empty,
            "Source endpoint:",
            "v1/championship_teams",
            string.Empty,
            "Standings table integration is next up.");
    }

    private static string BuildDriverStandingsTableText(
        int? selectedSeason,
        F1.Core.Models.Race? selectedRace,
        IReadOnlyList<F1.Core.Models.DriverStanding> standings)
    {
        var lines = new List<string>
        {
            "Driver Championship Standings",
            string.Empty,
            $"Season: {selectedSeason?.ToString() ?? "n/a"}",
            $"After: {selectedRace?.GrandPrixName ?? "selected race"}",
            string.Empty,
            "Pos  Driver                    Team                     Points",
            "---  ------------------------  -----------------------  ------"
        };

        foreach (var standing in standings)
        {
            lines.Add($"{standing.Position,3}  {Truncate(standing.DriverName, 24),-24}  {Truncate(standing.TeamName, 23),-23}  {standing.Points,6:0}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildConstructorStandingsTableText(
        int? selectedSeason,
        F1.Core.Models.Race? selectedRace,
        IReadOnlyList<F1.Core.Models.ConstructorStanding> standings)
    {
        var lines = new List<string>
        {
            "Constructor Championship Standings",
            string.Empty,
            $"Season: {selectedSeason?.ToString() ?? "n/a"}",
            $"After: {selectedRace?.GrandPrixName ?? "selected race"}",
            string.Empty,
            "Pos  Team                     Points",
            "---  -----------------------  ------"
        };

        foreach (var standing in standings)
        {
            lines.Add($"{standing.Position,3}  {Truncate(standing.TeamName, 23),-23}  {standing.Points,6:0}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, Math.Max(maxLength - 1, 0)) + "~";
    }

    private static string BuildDefaultShortcutsText()
    {
        return "[Enter] Select  [Esc] Back  [Q] Quit";
    }

    private static string BuildSessionDetailShortcutsText()
    {
        return "[1-2][Left/Right] Tab  [Up/Down] Scroll  [Esc] Back  [Q] Quit";
    }

    private static string BuildRaceHubShortcutsText()
    {
        return "[1-3][Left/Right] Tab  [Up/Down] Scroll  [Enter] Session Detail  [Esc] Back  [Q] Quit";
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
