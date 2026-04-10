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
        RaceResults = 1,
        Weather = 2,
        PitStops = 3
    }

    private readonly ISeasonService seasonService;
    private readonly IRaceService raceService;
    private readonly ISessionService sessionService;
    private readonly IRaceResultsService raceResultsService;
    private readonly IDriverStandingsService driverStandingsService;
    private readonly IConstructorStandingsService constructorStandingsService;
    private readonly IWeatherService weatherService;
    private readonly IPitStopService pitStopService;
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
        IWeatherService weatherService,
        IPitStopService pitStopService,
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
        this.weatherService = weatherService;
        this.pitStopService = pitStopService;
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

        stateStore.Update(state =>
            AppStateTransitions.ToSeasons(state, null, BuildSeasonsStatusMessage(null)));

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
                    var mergedSeasons = seasons
                        .Select(season => season.Year)
                        .Union(fetchedSeasons.Select(season => season.Year))
                        .OrderByDescending(year => year)
                        .Select(year => new F1.Core.Models.Season(year))
                        .ToList();

                    var mergedNames = mergedSeasons.Select(season => season.Year.ToString()).ToList();
                    var currentNames = seasons.Select(season => season.Year.ToString()).ToList();
                    if (mergedNames.SequenceEqual(currentNames))
                    {
                        return;
                    }

                    seasons = mergedSeasons;
                    seasonListView.SetSource(mergedNames);
                    statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
                });
            }
            catch
            {
            }
        }, cancellationToken);

        var raceRows = new List<string>();
        var sessionRows = new List<string>();
        var raceRowsAreData = false;
        var sessionRowsAreData = false;

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
        var raceResultsLines = new List<string>();
        var raceResultsOffset = 0;
        var pitSummaryLines = new List<string>();
        var pitSummaryOffset = 0;

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

        void RenderRaceResultsViewport()
        {
            if (raceResultsLines.Count == 0)
            {
                sessionDetailView.Text = string.Empty;
                return;
            }

            var viewportHeight = Math.Max(sessionDetailView.Bounds.Height, 5);
            var maxOffset = Math.Max(raceResultsLines.Count - viewportHeight, 0);
            raceResultsOffset = Math.Clamp(raceResultsOffset, 0, maxOffset);

            var visibleLines = raceResultsLines
                .Skip(raceResultsOffset)
                .Take(viewportHeight)
                .ToList();

            sessionDetailView.Text = string.Join(Environment.NewLine, visibleLines);
        }

        void RenderPitSummaryViewport()
        {
            if (pitSummaryLines.Count == 0)
            {
                sessionDetailView.Text = string.Empty;
                return;
            }

            var viewportHeight = Math.Max(sessionDetailView.Bounds.Height, 5);
            var maxOffset = Math.Max(pitSummaryLines.Count - viewportHeight, 0);
            pitSummaryOffset = Math.Clamp(pitSummaryOffset, 0, maxOffset);

            var visibleLines = pitSummaryLines
                .Skip(pitSummaryOffset)
                .Take(viewportHeight)
                .ToList();

            sessionDetailView.Text = string.Join(Environment.NewLine, visibleLines);
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

                    if (standings.Count == 0)
                    {
                        driverStandingsLines = SplitLines("No driver standings available for this race yet.");
                        driverStandingsOffset = 0;
                        RenderDriverStandingsViewport();
                        return;
                    }

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
                    driverStandingsLines = SplitLines("Unable to load driver standings right now.");
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

                    if (standings.Count == 0)
                    {
                        constructorStandingsView.Text = "No constructor standings available for this race yet.";
                        return;
                    }

                    constructorStandingsView.Text = BuildConstructorStandingsTableText(
                        stateStore.Current.SelectedSeason,
                        detailRace,
                        standings);
                }
                catch
                {
                    constructorStandingsView.Text = "Unable to load constructor standings right now.";
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

                    if (results.Count == 0)
                    {
                        raceResultsLines = SplitLines("No race results available for this session yet.");
                        raceResultsOffset = 0;
                        RenderRaceResultsViewport();
                        return;
                    }

                    raceResultsLines = SplitLines(BuildRaceResultsTableText(
                        stateStore.Current.SelectedSeason,
                        detailRace,
                        results));
                    raceResultsOffset = 0;
                    RenderRaceResultsViewport();
                }
                catch
                {
                    raceResultsLines = SplitLines("Unable to load race results right now.");
                    raceResultsOffset = 0;
                    RenderRaceResultsViewport();
                }

                return;
            }

            if (selectedDetailTab == SessionDetailTab.Weather)
            {
                sessionDetailView.Text = "Loading weather...";
                var items = await weatherService.GetWeatherSamplesAsync(detailSession.MeetingKey, detailSession.SessionKey, cancellationToken);
                sessionDetailView.Text = BuildWeatherSummaryText(items);
                raceResultsLines.Clear();
                pitSummaryLines.Clear();
                return;
            }

            if (selectedDetailTab == SessionDetailTab.PitStops)
            {
                sessionDetailView.Text = "Loading pit stops...";
                var items = await pitStopService.GetPitStopsAsync(detailSession.MeetingKey, detailSession.SessionKey, cancellationToken);
                pitSummaryLines = SplitLines(BuildPitStopsSummaryText(items));
                pitSummaryOffset = 0;
                RenderPitSummaryViewport();
                raceResultsLines.Clear();
                return;
            }

            raceResultsLines.Clear();
            raceResultsOffset = 0;
            pitSummaryLines.Clear();
            pitSummaryOffset = 0;

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
                raceRowsAreData = raceModels.Count > 0;
                raceRows = raceRowsAreData
                    ? raceModels
                        .OrderBy(race => race.RoundNumber)
                        .Select(race => $"R{race.RoundNumber}  {race.GrandPrixName}")
                        .ToList()
                    : ["Loading or no race data available yet."];
                raceListView.SetSource(raceRows);
                seasonListView.Visible = false;
                raceListView.Visible = true;
                title.Text = $"Races - {selectedSeason}";

                stateStore.Update(state =>
                    AppStateTransitions.ToRaces(
                        state,
                        selectedSeason,
                        null,
                        BuildRaceStatusMessage(selectedSeason, null)));

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

                stateStore.Update(state =>
                    AppStateTransitions.ToSeasons(
                        state,
                        state.SelectedSeason,
                        BuildSeasonsStatusMessage(state.SelectedSeason)));

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

                if (!raceRowsAreData)
                {
                    statusLine.Text = "Race data is still loading or unavailable.";
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

                sessionRowsAreData = sessionModels.Count > 0;
                sessionRows = sessionRowsAreData
                    ? sessionModels
                        .Select(session => session.SessionName)
                        .ToList()
                    : ["Loading or no session data available yet."];
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

                stateStore.Update(state =>
                    AppStateTransitions.ToSessions(
                        state,
                        selectedRace.RoundNumber,
                        selectedRace.GrandPrixName,
                        BuildSessionsStatusMessage(
                            state.SelectedSeason,
                            selectedRace.RoundNumber,
                            selectedRace.GrandPrixName,
                            null)));

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

                if (!sessionRowsAreData)
                {
                    statusLine.Text = "Session data is still loading or unavailable.";
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

                stateStore.Update(state =>
                    AppStateTransitions.ToSessionDetail(
                        state,
                        selectedSessionModel.SessionName,
                        BuildSessionsStatusMessage(
                            state.SelectedSeason,
                            selectedSessionModel.RoundNumber,
                            state.SelectedGrandPrixName,
                            selectedSessionModel.SessionName)));

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
                title.Text = $"Races - {stateStore.Current.SelectedSeason?.ToString() ?? "N/A"}";
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
                title.Text = $"Races - {stateStore.Current.SelectedSeason?.ToString() ?? "N/A"}";
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
                title.Text = $"Races - {stateStore.Current.SelectedSeason?.ToString() ?? "N/A"}";
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

            if (selectedDetailTab == SessionDetailTab.RaceResults && args.KeyEvent.Key == Key.CursorDown)
            {
                args.Handled = true;
                raceResultsOffset += 1;
                RenderRaceResultsViewport();
            }

            if (selectedDetailTab == SessionDetailTab.RaceResults && args.KeyEvent.Key == Key.CursorUp)
            {
                args.Handled = true;
                raceResultsOffset -= 1;
                RenderRaceResultsViewport();
            }

            if (selectedDetailTab == SessionDetailTab.PitStops && args.KeyEvent.Key == Key.CursorDown)
            {
                args.Handled = true;
                pitSummaryOffset += 1;
                RenderPitSummaryViewport();
            }

            if (selectedDetailTab == SessionDetailTab.PitStops && args.KeyEvent.Key == Key.CursorUp)
            {
                args.Handled = true;
                pitSummaryOffset -= 1;
                RenderPitSummaryViewport();
            }

            if (args.KeyEvent.Key == Key.CursorLeft)
            {
                args.Handled = true;
                selectedDetailTab = selectedDetailTab == SessionDetailTab.SessionInfo
                    ? SessionDetailTab.PitStops
                    : selectedDetailTab - 1;
                await RefreshDetailTabAsync();
            }

            if (args.KeyEvent.Key == Key.CursorRight)
            {
                args.Handled = true;
                selectedDetailTab = selectedDetailTab == SessionDetailTab.PitStops
                    ? SessionDetailTab.SessionInfo
                    : selectedDetailTab + 1;
                await RefreshDetailTabAsync();
            }

            if (args.KeyEvent.KeyValue >= '1' && args.KeyEvent.KeyValue <= '4')
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
        return $"Season {selectedSeason?.ToString() ?? "N/A"} | {grandPrix} | Data: {options.Value.ApiBaseUrl}";
    }

    private string BuildSessionsStatusMessage(
        int? selectedSeason,
        int? selectedRoundNumber,
        string? selectedGrandPrixName,
        string? selectedSession)
    {
        var roundText = selectedRoundNumber?.ToString() ?? "N/A";
        var grandPrix = string.IsNullOrWhiteSpace(selectedGrandPrixName) ? "N/A" : selectedGrandPrixName;
        var sessionName = string.IsNullOrWhiteSpace(selectedSession) ? "Select Session" : selectedSession;
        return $"Season {selectedSeason?.ToString() ?? "N/A"} | R{roundText} | {grandPrix} | {sessionName}";
    }

    private static string BuildSessionDetailTextForTab(
        SessionDetailTab tab,
        int? selectedSeason,
        F1.Core.Models.Race? selectedRace,
        F1.Core.Models.Session selectedSession)
    {
        if (tab == SessionDetailTab.RaceResults)
        {
            return "Loading race results...";
        }

        var seasonText = selectedSeason?.ToString() ?? "N/A";
        var roundText = selectedSession.RoundNumber.ToString();
        var grandPrixText = selectedRace?.GrandPrixName ?? "N/A";
        var locationText = selectedRace?.Location ?? "N/A";
        var startText = selectedSession.StartTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz") ?? "N/A";
        var endText = selectedSession.EndTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz") ?? "N/A";
        return string.Join(
            Environment.NewLine,
            "Session Information",
            string.Empty,
            $"Season: {seasonText}",
            $"Round: {roundText}",
            $"Grand Prix: {grandPrixText}",
            $"Location: {locationText}",
            $"Session: {selectedSession.SessionName}",
            string.Empty,
            $"Start: {startText}",
            $"End: {endText}");
    }

    private static string BuildSessionDetailTabsHeader(SessionDetailTab selectedTab)
    {
        return string.Join(
            "  ",
            BuildTabLabel(SessionDetailTab.SessionInfo, selectedTab, "1) Session Info"),
            BuildTabLabel(SessionDetailTab.RaceResults, selectedTab, "2) Results"),
            BuildTabLabel(SessionDetailTab.Weather, selectedTab, "3) Weather"),
            BuildTabLabel(SessionDetailTab.PitStops, selectedTab, "4) Pit"));
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

    private static string BuildRaceResultsTableText(
        int? selectedSeason,
        F1.Core.Models.Race? selectedRace,
        IReadOnlyList<F1.Core.Models.RaceResult> results)
    {
        var lines = new List<string>
        {
            "Race Results",
            string.Empty,
            $"Season: {selectedSeason?.ToString() ?? "N/A"}",
            $"Grand Prix: {selectedRace?.GrandPrixName ?? "N/A"}",
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

    private static string BuildDriverStandingsTableText(
        int? selectedSeason,
        F1.Core.Models.Race? selectedRace,
        IReadOnlyList<F1.Core.Models.DriverStanding> standings)
    {
        var lines = new List<string>
        {
            "Driver Championship Standings",
            string.Empty,
            $"Season: {selectedSeason?.ToString() ?? "N/A"}",
            $"After: {selectedRace?.GrandPrixName ?? "Selected Race"}",
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
            $"Season: {selectedSeason?.ToString() ?? "N/A"}",
            $"After: {selectedRace?.GrandPrixName ?? "Selected Race"}",
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

    private static string BuildWeatherSummaryText(IReadOnlyList<F1.Core.Models.WeatherSample> samples)
    {
        if (samples.Count == 0)
        {
            return "No weather data available for this session.";
        }

        var latest = samples
            .OrderByDescending(sample => sample.Timestamp ?? DateTimeOffset.MinValue)
            .First();

        var avgAir = samples.Where(sample => sample.AirTemperature.HasValue).Select(sample => sample.AirTemperature!.Value).DefaultIfEmpty().Average();
        var avgTrack = samples.Where(sample => sample.TrackTemperature.HasValue).Select(sample => sample.TrackTemperature!.Value).DefaultIfEmpty().Average();
        var rainySamples = samples.Count(sample => (sample.Rainfall ?? 0) > 0);

        return string.Join(
            Environment.NewLine,
            "Weather Summary",
            string.Empty,
            $"Samples: {samples.Count}",
            $"Average Air Temp: {avgAir:0.0} C",
            $"Average Track Temp: {avgTrack:0.0} C",
            $"Rainy Samples: {rainySamples}",
            string.Empty,
            $"Latest Sample: {latest.Timestamp?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "N/A"}",
            $"Latest Air Temp: {(latest.AirTemperature.HasValue ? latest.AirTemperature.Value.ToString("0.0") : "N/A")}",
            $"Latest Track Temp: {(latest.TrackTemperature.HasValue ? latest.TrackTemperature.Value.ToString("0.0") : "N/A")}",
            $"Latest Rainfall: {(latest.Rainfall.HasValue ? latest.Rainfall.Value.ToString("0.0") : "N/A")}");
    }

    private static string BuildPitStopsSummaryText(IReadOnlyList<F1.Core.Models.PitStop> pitStops)
    {
        if (pitStops.Count == 0)
        {
            return "No pit stop data available for this session.";
        }

        var fastest = pitStops
            .Where(pit => pit.PitDuration.HasValue)
            .OrderBy(pit => pit.PitDuration!.Value)
            .FirstOrDefault();

        var totalByDriver = pitStops
            .Where(pit => pit.DriverNumber.HasValue)
            .GroupBy(pit => pit.DriverNumber!.Value)
            .Select(group => new { Driver = group.Key, Count = group.Count() })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Driver)
            .ToList();

        var lines = new List<string>
        {
            "Pit Stop Summary",
            string.Empty,
            $"Total Pit Events: {pitStops.Count}",
            fastest is null ? "Fastest Pit: N/A" : $"Fastest Pit: {FormatDriverNameOnly(fastest.DriverName)} in {fastest.PitDuration:0.000}s",
            string.Empty,
            "Driver Pit Stop Counts:",
            "Driver                  Team                   Stops",
            "----------------------  ---------------------  -----"
        };

        var driverMeta = pitStops
            .Where(pit => pit.DriverNumber.HasValue)
            .GroupBy(pit => pit.DriverNumber!.Value)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Name = group.Select(pit => pit.DriverName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? "Unknown Driver",
                    Team = group.Select(pit => pit.TeamName).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? "Unknown Team"
                });

        lines.AddRange(totalByDriver.Select(item =>
        {
            var meta = driverMeta.TryGetValue(item.Driver, out var value)
                ? value
                : new { Name = "Unknown Driver", Team = "Unknown Team" };

            return $"{Truncate(meta.Name, 22),-22}  {Truncate(meta.Team, 21),-21}  {item.Count,5}";
        }));
        return string.Join(Environment.NewLine, lines);
    }

    private static string FormatDriverNameOnly(string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return "Unknown Driver";
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, Math.Max(maxLength - 1, 0)) + "~";
    }

    private static List<string> SplitLines(string content)
    {
        return content
            .Replace("\r", string.Empty)
            .Split('\n')
            .ToList();
    }

    private static string BuildDefaultShortcutsText()
    {
        return "[Enter] Select  [Esc] Back  [Q] Quit";
    }

    private static string BuildSessionDetailShortcutsText()
    {
        return "[1-4][Left/Right] Tab  [Up/Down] Scroll  [Esc] Back  [Q] Quit";
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
