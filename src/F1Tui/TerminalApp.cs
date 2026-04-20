using F1.Core.Services;
using F1Tui.Configuration;
using F1Tui.State;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Terminal.Gui;

namespace F1Tui;

public sealed class TerminalApp
{
    // Enum
    private enum DetailHubTab
    {
        SessionInfo          = 0,
        RaceResults          = 1,
        Weather              = 2,
        PitStops             = 3,
        DriverStandings      = 4,
        ConstructorStandings = 5,
        TeamsDrivers         = 6
    }

    private readonly ISeasonService seasonService;
    private readonly IRaceService raceService;
    private readonly ISessionService sessionService;
    private readonly IRaceResultsService raceResultsService;
    private readonly IDriverStandingsService driverStandingsService;
    private readonly IConstructorStandingsService constructorStandingsService;
    private readonly IWeatherService weatherService;
    private readonly IPitStopService pitStopService;
    private readonly IDriverService driverService;
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
        IDriverService driverService,
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
        this.driverService = driverService;
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

        Application.Init();

        // Color Schemes
        var listScheme = new ColorScheme
        {
            Normal    = Application.Driver.MakeAttribute(Color.White,     Color.Black),
            Focus     = Application.Driver.MakeAttribute(Color.Black,     Color.BrightRed),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            HotFocus  = Application.Driver.MakeAttribute(Color.Black,     Color.BrightRed),
            Disabled  = Application.Driver.MakeAttribute(Color.Gray,      Color.Black)
        };

        var darkScheme = new ColorScheme
        {
            Normal    = Application.Driver.MakeAttribute(Color.White,     Color.Black),
            Focus     = Application.Driver.MakeAttribute(Color.White,     Color.Black),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            HotFocus  = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            Disabled  = Application.Driver.MakeAttribute(Color.Gray,      Color.Black)
        };

        // Panel Scheme
        var panelScheme = new ColorScheme
        {
            Normal    = Application.Driver.MakeAttribute(Color.White,     Color.Black),
            Focus     = Application.Driver.MakeAttribute(Color.White,     Color.Black),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            HotFocus  = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            Disabled  = Application.Driver.MakeAttribute(Color.Gray,      Color.Black)
        };

        var footerScheme = new ColorScheme
        {
            Normal    = Application.Driver.MakeAttribute(Color.Gray,      Color.Black),
            Focus     = Application.Driver.MakeAttribute(Color.White,     Color.Black),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            HotFocus  = Application.Driver.MakeAttribute(Color.White,     Color.Red),
            Disabled  = Application.Driver.MakeAttribute(Color.Gray,      Color.Black)
        };

        // Shortcut Key Scheme
        var shortcutKeyScheme = new ColorScheme
        {
            Normal    = Application.Driver.MakeAttribute(Color.Black, Color.BrightRed),
            Focus     = Application.Driver.MakeAttribute(Color.Black, Color.BrightRed),
            HotNormal = Application.Driver.MakeAttribute(Color.Black, Color.BrightRed),
            HotFocus  = Application.Driver.MakeAttribute(Color.Black, Color.BrightRed),
            Disabled  = Application.Driver.MakeAttribute(Color.Gray,  Color.Black)
        };

        var shortcutDescScheme = new ColorScheme
        {
            Normal    = Application.Driver.MakeAttribute(Color.White, Color.Black),
            Focus     = Application.Driver.MakeAttribute(Color.White, Color.Black),
            HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Black),
            HotFocus  = Application.Driver.MakeAttribute(Color.White, Color.Black),
            Disabled  = Application.Driver.MakeAttribute(Color.Gray,  Color.Black)
        };

        // Root Window
        var top = Application.Top;
        top.ColorScheme = darkScheme;

        // Header
        const int headerContentHeight = 3;
        const int headerHeight = headerContentHeight + 2;

        var appNameScheme = new ColorScheme
        {
            Normal    = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            Focus     = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            HotFocus  = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            Disabled  = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black)
        };

        var headerBar = new FrameView("")
        {
            X = 0, Y = 0,
            Width  = Dim.Fill(),
            Height = headerHeight
        };
        headerBar.ColorScheme = panelScheme;

        var headerLine1 = new Label("_______                       _______")
        {
            X = Pos.Center(), Y = 0, Width = 42, Height = 1
        };
        headerLine1.ColorScheme = appNameScheme;

        var headerLine2 = new Label(" _\\=.o.=/_        PitTerm      _\\=.o.=/_")
        {
            X = Pos.Center(), Y = 1, Width = 45, Height = 1
        };
        headerLine2.ColorScheme = appNameScheme;

        var headerLine3 = new Label("|_|_____|_|        jntm7      |_|_____|_|")
        {
            X = Pos.Center(), Y = 2, Width = 45, Height = 1
        };
        headerLine3.ColorScheme = appNameScheme;

        headerBar.Add(headerLine1, headerLine2, headerLine3);

        // Panes
        var leftPane = new FrameView("[ Seasons ]")
        {
            X = 0, Y = headerHeight,
            Width  = Dim.Percent(50),
            Height = Dim.Fill() - 3
        };
        leftPane.ColorScheme = panelScheme;

        var rightPane = new FrameView("[ — ]")
        {
            X = Pos.Percent(50), Y = headerHeight,
            Width  = Dim.Percent(50),
            Height = Dim.Fill() - 3
        };
        rightPane.ColorScheme = panelScheme;

        void SetPaneRatio(int leftPercent)
        {
            leftPane.Width  = Dim.Percent(leftPercent);
            rightPane.X     = Pos.Percent(leftPercent);
            rightPane.Width = Dim.Percent(100 - leftPercent);
        }

        // Left Pane
        var seasonListView = new ListView(new List<string>())
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        seasonListView.ColorScheme = listScheme;
        leftPane.Add(seasonListView);

        var raceListView = new ListView(new List<string>())
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Visible = false
        };
        raceListView.ColorScheme = listScheme;
        leftPane.Add(raceListView);

        // Hub Tab List
        var hubTabItems = new List<string>
        {
            "  Session Info",
            "  Race Results",
            "  Weather",
            "  Pit Stops",
            "  Driver Standings",
            "  Constructor Standings",
            "  Teams & Drivers"
        };
        var detailTabListView = new ListView(hubTabItems)
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(),
            Visible = false
        };
        detailTabListView.ColorScheme = listScheme;
        leftPane.Add(detailTabListView);

        var teamDriverListView = new ListView(new List<string>())
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(),
            Visible = false
        };
        teamDriverListView.ColorScheme = listScheme;
        leftPane.Add(teamDriverListView);

// Right Pane
        var sessionListView = new ListView(new List<string>())
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(),
            Visible = false
        };
        sessionListView.ColorScheme = listScheme;
        rightPane.Add(sessionListView);

        var driversListView = new ListView(new List<string>())
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(),
            Visible = false
        };
        driversListView.ColorScheme = listScheme;
        rightPane.Add(driversListView);

        var teamListView = new ListView(new List<string>())
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(),
            Visible = false
        };
        teamListView.ColorScheme = listScheme;
        rightPane.Add(teamListView);

        // Content View
        var rightContentView = new TextView
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(),
            ReadOnly = true, WordWrap = false,
            Visible = false, Text = string.Empty
        };
        rightContentView.ColorScheme = darkScheme;
        rightPane.Add(rightContentView);

        // Footer
        var footerFrame = new FrameView("")
        {
            X = 0, Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(), Height = 3
        };
        footerFrame.ColorScheme = footerScheme;

        // Shortcut Bar
        var shortcutBar = new View
        {
            X = 0, Y = 0, Width = Dim.Fill(), Height = 1
        };
        shortcutBar.ColorScheme = footerScheme;

        footerFrame.Add(shortcutBar);
        top.Add(headerBar, leftPane, rightPane, footerFrame);

        // Mutable State
        var seasons            = new List<F1.Core.Models.Season>();
        var raceModels         = new List<F1.Core.Models.Race>();
        var sessionModels      = new List<F1.Core.Models.Session>();
        var raceRows           = new List<string>();
        var sessionRows        = new List<string>();
        var teamRows           = new List<string>();
        var teamModels         = new List<F1.Core.Models.Driver>();
        var teamDetailDrivers  = new List<F1.Core.Models.Driver>();
        var raceRowsAreData    = false;
        var sessionRowsAreData = false;
        var teamRowsAreData    = false;

        F1.Core.Models.Race?    activeRace    = null;
        F1.Core.Models.Session? activeSession = null;
        var selectedDetailTab = DetailHubTab.SessionInfo;
        var showTeamsOnRight = false;


        var contentLines  = new List<string>();
        var contentOffset = 0;

        // Shortcut Bar Helper
        void RebuildShortcutBar(IReadOnlyList<(string key, string desc)> shortcuts)
        {
            foreach (var v in shortcutBar.Subviews.ToList())
                shortcutBar.Remove(v);

            var x = 0;
            foreach (var (key, desc) in shortcuts)
            {
                shortcutBar.Add(new Label($" {key} ")
                {
                    X = x, Y = 0, Width = key.Length + 2, Height = 1,
                    ColorScheme = shortcutKeyScheme
                });
                x += key.Length + 2;

                shortcutBar.Add(new Label($" {desc}  ")
                {
                    X = x, Y = 0, Width = desc.Length + 3, Height = 1,
                    ColorScheme = shortcutDescScheme
                });
                x += desc.Length + 3;
            }
            shortcutBar.SetNeedsDisplay();
        }

        // Content Viewport Helper
        void RenderContentViewport()
        {
            if (contentLines.Count == 0) { rightContentView.Text = string.Empty; return; }
            var h = Math.Max(rightContentView.Bounds.Height, 5);
            contentOffset = Math.Clamp(contentOffset, 0, Math.Max(contentLines.Count - h, 0));
            rightContentView.Text = string.Join(Environment.NewLine,
                contentLines.Skip(contentOffset).Take(h));
        }

        static string HubTabTitle(DetailHubTab tab) => tab switch
        {
            DetailHubTab.SessionInfo          => "[ Session Info ]",
            DetailHubTab.RaceResults          => "[ Race Results ]",
            DetailHubTab.Weather              => "[ Weather ]",
            DetailHubTab.PitStops             => "[ Pit Stops ]",
            DetailHubTab.DriverStandings      => "[ Driver Standings ]",
            DetailHubTab.ConstructorStandings => "[ Constructor Standings ]",
            DetailHubTab.TeamsDrivers         => "[ Teams & Drivers ]",
            _                                 => "[ — ]"
        };

        // Hub Content Loader
        void StartHubContentLoad(DetailHubTab tab)
        {
            var capturedTab     = tab;
            var capturedSession = activeSession;
            var capturedRace    = activeRace;
            var capturedSeason  = stateStore.Current.SelectedSeason;

            rightPane.Title = HubTabTitle(tab);
            rightContentView.Text = "Loading...";
            Application.Refresh();

            _ = Task.Run(async () =>
            {
                List<string> lines;
                try
                {
                    lines = capturedTab switch
                    {
                        DetailHubTab.SessionInfo => SplitLines(
                            BuildSessionInfoText(capturedSeason, capturedRace, capturedSession!)),

                        DetailHubTab.RaceResults => await LoadRaceResultsLinesAsync(
                            capturedSeason, capturedRace, capturedSession!, cancellationToken),

                        DetailHubTab.Weather => await LoadWeatherLinesAsync(
                            capturedSession!, cancellationToken),

                        DetailHubTab.PitStops => await LoadPitStopsLinesAsync(
                            capturedSession!, cancellationToken),

                        DetailHubTab.DriverStandings => await LoadDriverStandingsLinesAsync(
                            capturedSeason, capturedRace, capturedSession!, cancellationToken),

                        DetailHubTab.ConstructorStandings => await LoadConstructorStandingsLinesAsync(
                            capturedSeason, capturedRace, capturedSession!, cancellationToken),

                        DetailHubTab.TeamsDrivers => await LoadTeamsDriversLinesAsync(
                            capturedSeason, capturedRace, cancellationToken),

                        _ => ["No content."]
                    };
                }
                catch
                {
                    lines = ["Unable to load data. Please try again."];
                }

                Application.MainLoop?.Invoke(() =>
                {
                    if (selectedDetailTab != capturedTab) return;
                    contentLines  = lines;
                    contentOffset = 0;
                    RenderContentViewport();
                    Application.Refresh();
                });
            }, cancellationToken);
        }

        void StartTeamsLoad(int year)
        {
            teamRows = ["Loading teams..."];
            teamRowsAreData = false;
            teamListView.SetSource(teamRows);
            teamListView.Visible = true;
            rightPane.Title = $"[ Teams & Drivers ({year}) ]";

            _ = Task.Run(async () =>
            {
                List<string> rows;
                List<F1.Core.Models.Driver> models;
                var hasData = false;
                try
                {
                    var races = (await raceService.GetRacesBySeasonAsync(year, cancellationToken)).ToList();
                    var latestMeeting = races
                        .Where(r => r.MeetingKey.HasValue)
                        .OrderByDescending(r => r.RoundNumber)
                        .FirstOrDefault()?.MeetingKey;

                    models = (await driverService.GetDriversAsync(latestMeeting, cancellationToken))
                        .Where(d => !string.IsNullOrWhiteSpace(d.TeamName)
                                    && !string.Equals(d.TeamName, "Unknown Team", StringComparison.OrdinalIgnoreCase))
                        .GroupBy(d => d.DriverNumber)
                        .Select(g => g.First())
                        .OrderBy(d => d.TeamName)
                        .ThenBy(d => d.DriverNumber)
                        .ToList();

                    var teams = models
                        .Select(d => d.TeamName)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(t => t)
                        .ToList();

                    hasData = teams.Count > 0;
                    rows = hasData ? teams : ["No team data available."];
                }
                catch
                {
                    models = [];
                    rows = ["Error loading teams."];
                }

                Application.MainLoop?.Invoke(() =>
                {
                    teamRowsAreData = hasData;
                    teamRows = rows;
                    teamModels = models;
                    teamListView.SetSource(teamRows);
                    teamListView.Visible = showTeamsOnRight;
                    rightPane.Title = $"[ Teams & Drivers ({year}) ]";
                    Application.Refresh();
                });
            }, cancellationToken);
        }

        // Screen Transitions

        void GoToSeasonsScreen()
        {
            SetPaneRatio(50);
            seasonListView.Visible    = true;
            raceListView.Visible      = false;
            detailTabListView.Visible = false;
            teamDriverListView.Visible = false;
            leftPane.Title = "[ Seasons ]";

            sessionListView.Visible  = false;
            driversListView.Visible   = false;
            rightContentView.Visible = false;
            showTeamsOnRight = true;

            var year = stateStore.Current.SelectedSeason ?? DateTime.UtcNow.Year;
            StartTeamsLoad(year);

            RebuildShortcutBar(SeasonsShortcuts());
            seasonListView.SetFocus();
        }

        void GoToRacesScreen(int season)
        {
            SetPaneRatio(50);
            seasonListView.Visible    = false;
            raceListView.Visible      = true;
            detailTabListView.Visible = false;
            teamDriverListView.Visible = false;
            leftPane.Title = $"[ Races — {season} ]";

            sessionListView.Visible  = false;
            driversListView.Visible = false;
            teamListView.Visible   = false;
            rightContentView.Visible = false;
            rightPane.Title = "[ Select a Race ]";

            RebuildShortcutBar(RacesShortcuts());
            raceListView.SetFocus();
        }

        void GoToRaceSessionsScreen(string grandPrixName)
        {
            SetPaneRatio(50);
            sessionListView.Visible  = true;
            rightContentView.Visible = false;
            rightPane.Title = $"[ {Truncate(grandPrixName, 44)} ]";

            RebuildShortcutBar(SessionsShortcuts());
            sessionListView.SetFocus();
        }

        void GoToDetailHubScreen(F1.Core.Models.Session session, DetailHubTab tab)
        {
            SetPaneRatio(35);
            seasonListView.Visible    = false;
            raceListView.Visible      = false;
            detailTabListView.Visible = true;
            teamDriverListView.Visible = false;
            detailTabListView.SelectedItem = (int)tab;
            leftPane.Title = $"[ {session.SessionName} ]";

            sessionListView.Visible  = false;
            driversListView.Visible = false;
            teamListView.Visible   = false;
            rightContentView.Visible = true;

            RebuildShortcutBar(HubShortcuts());
            detailTabListView.SetFocus();
        }

        void ReturnFromHubToSessions()
        {
            SetPaneRatio(50);
            detailTabListView.Visible = false;
            raceListView.Visible      = true;
            teamDriverListView.Visible = false;
            leftPane.Title = $"[ Races — {stateStore.Current.SelectedSeason?.ToString() ?? "N/A"} ]";

            rightContentView.Visible = false;
            sessionListView.Visible  = true;
            rightPane.Title = $"[ {Truncate(activeRace?.GrandPrixName ?? "Sessions", 44)} ]";

            RebuildShortcutBar(SessionsShortcuts());
            sessionListView.SetFocus();
        }

        void GoToTeamDetailScreen(string teamName, int season)
        {
            SetPaneRatio(45);
            seasonListView.Visible    = false;
            raceListView.Visible      = false;
            detailTabListView.Visible = false;
            teamDriverListView.Visible = true;
            leftPane.Title = $"[ {teamName} Drivers ]";
            teamListView.Visible   = false;

            sessionListView.Visible   = false;
            driversListView.Visible   = false;
            rightContentView.Visible = true;
            rightPane.Title = "[ Driver Details ]";

            var teamDrivers = teamModels
                .Where(d => string.Equals(d.TeamName, teamName, StringComparison.OrdinalIgnoreCase))
                .GroupBy(d => d.DriverNumber)
                .Select(g => g.First())
                .ToList();

            teamDetailDrivers = teamDrivers
                .OrderBy(d => d.DriverNumber)
                .ToList();

            var rows = teamDetailDrivers
                .Select(d => $"{d.NameAcronym,-4}  {d.DriverNumber,3}  {d.FullName}")
                .ToList();

            teamDriverListView.SetSource(rows);
            teamDriverListView.SelectedItem = 0;
            if (teamDetailDrivers.Count > 0)
            {
                rightContentView.Text = BuildDriverDetailsText(teamDetailDrivers[0]);
            }
            else
            {
                rightContentView.Text = "No drivers found for this team.";
            }

            RebuildShortcutBar(TeamDetailShortcuts());
            teamDriverListView.SetFocus();
        }

        void ReturnFromTeamDetailToHome()
        {
            GoToSeasonsScreen();
        }

        top.KeyPress += args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }
        };

        void FocusHomeOppositePane()
        {
            if (!seasonListView.Visible || raceListView.Visible || detailTabListView.Visible || teamDriverListView.Visible)
            {
                return;
            }

            if (seasonListView.HasFocus)
            {
                if (showTeamsOnRight)
                {
                    teamListView.SetFocus();
                }
                return;
            }

            if (teamListView.HasFocus)
            {
                seasonListView.SetFocus();
            }
        }

        seasonListView.KeyPress += async args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            { args.Handled = true; Application.RequestStop(); return; }

            if (args.KeyEvent.Key == Key.Tab)
            {
                args.Handled = true;
                if (!showTeamsOnRight)
                {
                    showTeamsOnRight = true;
                    var year = stateStore.Current.SelectedSeason ?? DateTime.UtcNow.Year;
                    StartTeamsLoad(year);
                }
                FocusHomeOppositePane();
                return;
            }

            if (args.KeyEvent.Key != Key.Enter) return;
            args.Handled = true;

            if (seasonListView.SelectedItem < 0 || seasonListView.SelectedItem >= seasons.Count) return;
            var season = seasons[seasonListView.SelectedItem].Year;

            Application.Refresh();

            raceModels = (await raceService.GetRacesBySeasonAsync(season, cancellationToken)).ToList();
            raceRowsAreData = raceModels.Count > 0;
            raceRows = raceRowsAreData
                ? raceModels.OrderBy(r => r.RoundNumber)
                    .Select(r => $"R{r.RoundNumber,-2}  {r.GrandPrixName}")
                    .ToList()
                : ["No race data available yet."];

            raceListView.SetSource(raceRows);

            stateStore.Update(state =>
                AppStateTransitions.ToRaces(state, season, null, BuildRaceStatusMessage(season, null)));
            

            GoToRacesScreen(season);
        };

        // Race List Handlers
        raceListView.SelectedItemChanged += args =>
        {
            if (stateStore.Current.SelectedSeason is null) return;
            if (args.Item < 0 || args.Item >= raceModels.Count) return;
            var race = raceModels[args.Item];
            stateStore.Update(state => state with
            {
                SelectedRoundNumber   = race.RoundNumber,
                SelectedGrandPrixName = race.GrandPrixName,
                SelectedSessionName   = null,
                StatusMessage         = BuildRaceStatusMessage(state.SelectedSeason, race.GrandPrixName)
            });
            
        };

        raceListView.KeyPress += async args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            { args.Handled = true; Application.RequestStop(); return; }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;
                stateStore.Update(state =>
                    AppStateTransitions.ToSeasons(state, state.SelectedSeason,
                        BuildSeasonsStatusMessage(state.SelectedSeason)));
                
                GoToSeasonsScreen();
                return;
            }

            if (args.KeyEvent.Key != Key.Enter) return;
            args.Handled = true;

            if (raceListView.SelectedItem < 0 || raceListView.SelectedItem >= raceModels.Count) return;
            if (!raceRowsAreData) return;

            activeRace = raceModels[raceListView.SelectedItem];
            Application.Refresh();

            sessionModels = (await sessionService.GetSessionsByRaceAsync(
                activeRace.Season, activeRace.RoundNumber, activeRace.MeetingKey, cancellationToken)).ToList();

            sessionRowsAreData = sessionModels.Count > 0;
            sessionRows = sessionRowsAreData
                ? sessionModels
                    .Select(s =>
                        $"{s.SessionName,-28}  {s.StartTime?.ToLocalTime().ToString("MMM dd") ?? "TBD"}")
                    .ToList()
                : ["No session data available yet."];

            sessionListView.SetSource(sessionRows);

            stateStore.Update(state =>
                AppStateTransitions.ToSessions(state, activeRace.RoundNumber,
                    activeRace.GrandPrixName,
                    BuildSessionsStatusMessage(state.SelectedSeason, activeRace.RoundNumber,
                        activeRace.GrandPrixName, null)));
            

            GoToRaceSessionsScreen(activeRace.GrandPrixName);
        };

        // Session List Handlers
        sessionListView.SelectedItemChanged += args =>
        {
            if (args.Item < 0 || args.Item >= sessionRows.Count) return;
            var name = sessionRows[args.Item];
            stateStore.Update(state => state with
            {
                SelectedSessionName = name,
                StatusMessage       = BuildSessionsStatusMessage(
                    state.SelectedSeason, activeRace?.RoundNumber,
                    activeRace?.GrandPrixName, name)
            });
            
        };

        sessionListView.KeyPress += async args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            { args.Handled = true; Application.RequestStop(); return; }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;
                SetPaneRatio(50);
                sessionListView.Visible = false;
                rightPane.Title = "[ Select a race → ]";
                RebuildShortcutBar(RacesShortcuts());
                raceListView.SetFocus();
                return;
            }

            if (args.KeyEvent.Key != Key.Enter) return;
            args.Handled = true;

            if (sessionListView.SelectedItem < 0 || sessionListView.SelectedItem >= sessionModels.Count) return;
            if (!sessionRowsAreData) return;

            activeSession     = sessionModels[sessionListView.SelectedItem];
            selectedDetailTab = DetailHubTab.SessionInfo;

            stateStore.Update(state =>
                AppStateTransitions.ToSessionDetail(state, activeSession.SessionName,
                    BuildSessionsStatusMessage(state.SelectedSeason, activeSession.RoundNumber,
                        state.SelectedGrandPrixName, activeSession.SessionName)));
            

            GoToDetailHubScreen(activeSession, selectedDetailTab);
            StartHubContentLoad(selectedDetailTab);
        };

        // Hub Tab Handlers
        detailTabListView.SelectedItemChanged += args =>
        {
            selectedDetailTab = (DetailHubTab)args.Item;
            contentLines.Clear();
            contentOffset = 0;
            StartHubContentLoad(selectedDetailTab);
        };

        detailTabListView.KeyPress += args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            { args.Handled = true; Application.RequestStop(); return; }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;
                stateStore.Update(state => state with
                {
                    ActiveScreen  = "Sessions",
                    StatusMessage = BuildSessionsStatusMessage(
                        state.SelectedSeason, activeRace?.RoundNumber,
                        activeRace?.GrandPrixName, activeSession?.SessionName)
                });
                
                ReturnFromHubToSessions();
                return;
            }

            if (args.KeyEvent.Key == Key.Enter)
            {
                args.Handled = true;
                rightContentView.SetFocus();
            }
        };

        teamListView.KeyPress += args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            { args.Handled = true; Application.RequestStop(); return; }

            if (args.KeyEvent.Key == Key.Tab)
            {
                args.Handled = true;
                FocusHomeOppositePane();
                return;
            }

            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;
                showTeamsOnRight = false;
                teamListView.Visible = false;
                sessionListView.Visible = false;
                rightContentView.Visible = false;
                rightPane.Title = "[ — ]";
                RebuildShortcutBar(SeasonsShortcuts());
                seasonListView.SetFocus();
                return;
            }

            if (args.KeyEvent.Key != Key.Enter) return;
            args.Handled = true;

            if (teamListView.SelectedItem < 0 || teamListView.SelectedItem >= teamRows.Count) return;
            var selectedTeam = teamRows[teamListView.SelectedItem];
            var year = stateStore.Current.SelectedSeason ?? DateTime.UtcNow.Year;

            GoToTeamDetailScreen(selectedTeam, year);
            return;
        };

        teamDriverListView.SelectedItemChanged += args =>
        {
            if (args.Item < 0 || args.Item >= teamDetailDrivers.Count) return;
            rightContentView.Text = BuildDriverDetailsText(teamDetailDrivers[args.Item]);
            rightPane.Title = $"[ {teamDetailDrivers[args.Item].FullName} ]";
        };

        teamDriverListView.KeyPress += args =>
        {
            if (args.KeyEvent.Key == Key.Esc)
            {
                args.Handled = true;
                ReturnFromTeamDetailToHome();
                return;
            }

            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
                return;
            }
        };

        // Content Scroll
        rightContentView.KeyPress += args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                if (args.KeyEvent.Key == Key.Esc && teamDriverListView.Visible)
                {
                    ReturnFromTeamDetailToHome();
                    return;
                }
                Application.RequestStop();
                return;
            }

            if (args.KeyEvent.Key == Key.CursorDown)
            { args.Handled = true; contentOffset++; RenderContentViewport(); }

            if (args.KeyEvent.Key == Key.CursorUp)
            { args.Handled = true; contentOffset--; RenderContentViewport(); }

            if (args.KeyEvent.Key is Key.Esc or Key.Tab)
            {
                args.Handled = true;
                if (detailTabListView.Visible)
                {
                    detailTabListView.SetFocus();
                }
                else if (teamDriverListView.Visible)
                {
                    ReturnFromTeamDetailToHome();
                }
                else if (seasonListView.Visible && teamListView.Visible)
                {
                    FocusHomeOppositePane();
                }
            }
        };

        // Seasons Init
        var currentYear = DateTime.UtcNow.Year;
        seasons = Enumerable.Range(2023, Math.Max(currentYear - 2023 + 1, 1))
            .OrderByDescending(y => y)
            .Select(y => new F1.Core.Models.Season(y))
            .ToList();
        seasonListView.SetSource(seasons.Select(s => $"{s.Year} Season").ToList());
        seasonListView.Visible = true;

        stateStore.Update(state =>
            AppStateTransitions.ToSeasons(state, null, BuildSeasonsStatusMessage(null)));

        logger.LogInformation(
            "Initial app state: screen={Screen}, season={Season}",
            stateStore.Current.ActiveScreen,
            stateStore.Current.SelectedSeason);

        GoToSeasonsScreen();

        Application.Run();
        Application.Shutdown();
    }

    // Data Loaders

    private async Task<List<string>> LoadRaceResultsLinesAsync(
        int? season, F1.Core.Models.Race? race,
        F1.Core.Models.Session session, CancellationToken ct)
    {
        var results = await raceResultsService.GetRaceResultsAsync(
            season ?? session.Season, session.MeetingKey, session.SessionKey, ct);
        return results.Count == 0
            ? ["No race results available for this session yet."]
            : SplitLines(BuildRaceResultsTableText(season, race, results));
    }

    private async Task<List<string>> LoadWeatherLinesAsync(
        F1.Core.Models.Session session, CancellationToken ct)
    {
        var samples = await weatherService.GetWeatherSamplesAsync(
            session.MeetingKey, session.SessionKey, ct);
        return SplitLines(BuildWeatherSummaryText(samples));
    }

    private async Task<List<string>> LoadPitStopsLinesAsync(
        F1.Core.Models.Session session, CancellationToken ct)
    {
        var pits = await pitStopService.GetPitStopsAsync(
            session.MeetingKey, session.SessionKey, ct);
        return SplitLines(BuildPitStopsSummaryText(pits));
    }

    private async Task<List<string>> LoadDriverStandingsLinesAsync(
        int? season, F1.Core.Models.Race? race,
        F1.Core.Models.Session session, CancellationToken ct)
    {
        var standings = await driverStandingsService.GetDriverStandingsAsync(
            season ?? session.Season, race?.MeetingKey, ct);
        return standings.Count == 0
            ? ["No driver standings available for this race yet."]
            : SplitLines(BuildDriverStandingsTableText(season, race, standings));
    }

    private async Task<List<string>> LoadConstructorStandingsLinesAsync(
        int? season, F1.Core.Models.Race? race,
        F1.Core.Models.Session session, CancellationToken ct)
    {
        var standings = await constructorStandingsService.GetConstructorStandingsAsync(
            season ?? session.Season, race?.MeetingKey, ct);
        return standings.Count == 0
            ? ["No constructor standings available for this race yet."]
            : SplitLines(BuildConstructorStandingsTableText(season, race, standings));
    }

    private async Task<List<string>> LoadTeamsDriversLinesAsync(
        int? season, F1.Core.Models.Race? race, CancellationToken ct)
    {
        var drivers = await driverService.GetDriversAsync(race?.MeetingKey, ct);
        return drivers.Count == 0
            ? ["No driver roster data available."]
            : SplitLines(BuildTeamsDriversText(season, drivers));
    }

    // Shortcut Definitions

    private static IReadOnlyList<(string, string)> SeasonsShortcuts() =>
    [
        ("Enter", "Select"),
        ("Tab",   "Switch Pane"),
        ("Q",     "Quit")
    ];

    private static IReadOnlyList<(string, string)> RacesShortcuts() =>
    [
        ("Enter", "Open Race"),
        ("Esc",   "Seasons"),
        ("Q",     "Quit")
    ];

    private static IReadOnlyList<(string, string)> SessionsShortcuts() =>
    [
        ("Enter", "Open Session"),
        ("Esc",   "Races"),
        ("Q",     "Quit")
    ];

    private static IReadOnlyList<(string, string)> HubShortcuts() =>
    [
        ("↑/↓",  "Switch Tab"),
        ("Enter", "Scroll Mode"),
        ("Esc",   "Sessions"),
        ("Q",     "Quit")
    ];

    private static IReadOnlyList<(string, string)> TeamDetailShortcuts() =>
    [
        ("↑/↓",  "Select Driver"),
        ("Esc",   "Back"),
        ("Q",     "Quit")
    ];

    private static string BuildDriverDetailsText(F1.Core.Models.Driver driver)
    {
        var ageText = "N/A";
        if (driver.DateOfBirth.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - driver.DateOfBirth.Value.Year;
            if (driver.DateOfBirth.Value.DayOfYear > today.DayOfYear)
            {
                age--;
            }
            ageText = age.ToString();
        }

return string.Join(Environment.NewLine,
        [
            "Driver Details",
            "──────────────────────────────────",
            $"Name:            {driver.FullName}",
            $"Acronym:         {driver.NameAcronym}",
            $"Number:          {driver.DriverNumber}",
            $"Team:            {driver.TeamName}",
            $"Nationality:     {driver.Nationality}",
            $"DOB:             {driver.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}",
            $"Age:             {ageText}"
        ]);
    }

    // Status Messages

    private string BuildSeasonsStatusMessage(int? season) =>
        season is null
            ? $"Season: N/A  |  {options.Value.ApiBaseUrl}"
            : $"Season {season}  |  {options.Value.ApiBaseUrl}";

    private string BuildRaceStatusMessage(int? season, string? gp)
    {
        var grandPrix = string.IsNullOrWhiteSpace(gp) ? "Select Race" : gp;
        return $"Season {season?.ToString() ?? "N/A"}  |  {grandPrix}  |  {options.Value.ApiBaseUrl}";
    }

    private string BuildSessionsStatusMessage(
        int? season, int? round, string? gp, string? session)
    {
        var roundText   = round?.ToString() ?? "N/A";
        var grandPrix   = string.IsNullOrWhiteSpace(gp)      ? "N/A"            : gp;
        var sessionName = string.IsNullOrWhiteSpace(session)  ? "Select Session" : session;
        return $"Season {season?.ToString() ?? "N/A"}  |  R{roundText}  |  {grandPrix}  |  {sessionName}";
    }

    // Content Builders

    private static string BuildSessionInfoText(
        int? season, F1.Core.Models.Race? race, F1.Core.Models.Session session)
    {
        var lines = new List<string>
        {
            $"Season:     {season?.ToString() ?? "N/A"}",
            $"Round:      {session.RoundNumber}",
            $"Grand Prix: {race?.GrandPrixName ?? "N/A"}",
            $"Location:   {race?.Location ?? "N/A"}",
            $"Session:    {session.SessionName}",
            $"Start:      {session.StartTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz") ?? "N/A"}",
            $"End:        {session.EndTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm zzz") ?? "N/A"}"
        };
        return FormatSection("Session Information", lines);
    }

    private static string BuildRaceResultsTableText(
        int? season, F1.Core.Models.Race? race,
        IReadOnlyList<F1.Core.Models.RaceResult> results)
    {
        var lines = new List<string>
        {
            $"Season: {season?.ToString() ?? "N/A"}",
            $"Grand Prix: {race?.GrandPrixName ?? "N/A"}",
            string.Empty,
            "Pos  Driver               Team",
            new string('─', 42)
        };
        foreach (var r in results)
            lines.Add($"{r.Position,3}  {Truncate(r.DriverName, 20),-20}  {Truncate(r.TeamName, 15),-15}");
        return FormatSection("Race Results", lines);
    }

    private static string BuildDriverStandingsTableText(
        int? season, F1.Core.Models.Race? race,
        IReadOnlyList<F1.Core.Models.DriverStanding> standings)
    {
        var lines = new List<string>
        {
            $"Season: {season?.ToString() ?? "N/A"}",
            $"After:  {race?.GrandPrixName ?? "Selected Race"}",
            string.Empty,
            "Pos  Driver               Team                Points",
            new string('─', 54)
        };
        foreach (var s in standings)
            lines.Add($"{s.Position,3}  {Truncate(s.DriverName, 20),-20}  {Truncate(s.TeamName, 19),-19}  {s.Points,6:0}");
        return FormatSection("Driver Championship", lines);
    }

    private static string BuildConstructorStandingsTableText(
        int? season, F1.Core.Models.Race? race,
        IReadOnlyList<F1.Core.Models.ConstructorStanding> standings)
    {
        var lines = new List<string>
        {
            $"Season: {season?.ToString() ?? "N/A"}",
            $"After:  {race?.GrandPrixName ?? "Selected Race"}",
            string.Empty,
            "Pos  Team                 Points",
            new string('─', 33)
        };
        foreach (var s in standings)
            lines.Add($"{s.Position,3}  {Truncate(s.TeamName, 20),-20}  {s.Points,6:0}");
        return FormatSection("Constructor Championship", lines);
    }

    private static string BuildWeatherSummaryText(IReadOnlyList<F1.Core.Models.WeatherSample> samples)
    {
        if (samples.Count == 0)
            return FormatSection("Weather Summary", ["No weather data available for this session."]);

        var latest   = samples.OrderByDescending(s => s.Timestamp ?? DateTimeOffset.MinValue).First();
        var avgAir   = samples.Where(s => s.AirTemperature.HasValue)
            .Select(s => s.AirTemperature!.Value).DefaultIfEmpty().Average();
        var avgTrack = samples.Where(s => s.TrackTemperature.HasValue)
            .Select(s => s.TrackTemperature!.Value).DefaultIfEmpty().Average();
        var rainy    = samples.Count(s => (s.Rainfall ?? 0) > 0);

        return FormatSection("Weather Summary",
        [
            $"Samples:           {samples.Count}",
            $"Avg Air Temp:      {avgAir:0.0} °C",
            $"Avg Track Temp:    {avgTrack:0.0} °C",
            $"Rainy Samples:     {rainy}",
            string.Empty,
            $"Latest Sample:     {latest.Timestamp?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "N/A"}",
            $"Latest Air Temp:   {(latest.AirTemperature.HasValue   ? latest.AirTemperature.Value.ToString("0.0")   : "N/A")}",
            $"Latest Track Temp: {(latest.TrackTemperature.HasValue ? latest.TrackTemperature.Value.ToString("0.0") : "N/A")}",
            $"Latest Rainfall:   {(latest.Rainfall.HasValue         ? latest.Rainfall.Value.ToString("0.0")         : "N/A")}"
        ]);
    }

    private static string BuildPitStopsSummaryText(IReadOnlyList<F1.Core.Models.PitStop> pitStops)
    {
        if (pitStops.Count == 0)
            return FormatSection("Pit Stop Summary", ["No pit stop data available for this session."]);

        var fastest = pitStops
            .Where(p => p.PitDuration.HasValue)
            .OrderBy(p => p.PitDuration!.Value)
            .FirstOrDefault();

        var totalByDriver = pitStops
            .Where(p => p.DriverNumber.HasValue)
            .GroupBy(p => p.DriverNumber!.Value)
            .Select(g => new { Driver = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ThenBy(x => x.Driver)
            .ToList();

        var driverMeta = pitStops
            .Where(p => p.DriverNumber.HasValue)
            .GroupBy(p => p.DriverNumber!.Value)
            .ToDictionary(g => g.Key, g => new
            {
                Name = g.Select(p => p.DriverName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "Unknown",
                Team = g.Select(p => p.TeamName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))   ?? "Unknown"
            });

        var lines = new List<string>
        {
            $"Total Pit Events:  {pitStops.Count}",
            fastest is null
                ? "Fastest Pit:       N/A"
                : $"Fastest Pit:       {FormatDriverName(fastest.DriverName)} — {fastest.PitDuration:0.000}s",
            string.Empty,
            "Driver               Team                Stops",
            new string('─', 48)
        };

        lines.AddRange(totalByDriver.Select(item =>
        {
            var m = driverMeta.TryGetValue(item.Driver, out var v)
                ? v : new { Name = "Unknown", Team = "Unknown" };
            return $"{Truncate(m.Name, 20),-20}  {Truncate(m.Team, 19),-19}  {item.Count,5}";
        }));

        return FormatSection("Pit Stop Summary", lines);
    }

    private static string FormatSection(string title, IReadOnlyList<string> lines)
    {
        var content = lines.Count == 0 ? [string.Empty] : lines.ToList();
        var width = Math.Max(title.Length, content.Max(l => l.Length));
        var divider = new string('─', width);

        return string.Join(Environment.NewLine,
            new[] { title, divider }
            .Concat(content));
    }

    private static string BuildTeamsDriversText(
        int? season, IReadOnlyList<F1.Core.Models.Driver> drivers)
    {
        var grouped = drivers
            .GroupBy(d => d.TeamName)
            .OrderBy(g => g.Key)
            .ToList();

        var lines = new List<string>
        {
            $"Season: {season?.ToString() ?? "N/A"}",
            string.Empty
        };

        foreach (var team in grouped)
        {
            var teamColour = team.First().TeamColour;
            lines.Add($"{team.Key}  #{teamColour}");
            lines.Add($"  {"#",3}  {"Code",-4}  {"Driver",-24}");
            lines.Add($"  {"──",3}  {"────",-4}  {"────────────────────────",-24}");
            foreach (var d in team)
            {
                lines.Add($"  {d.DriverNumber,3}  {d.NameAcronym,-4}  {d.FullName,-24}");
            }
            lines.Add(string.Empty);
        }

        return FormatSection("Teams & Drivers", lines);
    }

    // Utilities

    private static string FormatDriverName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "Unknown Driver" : name;

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) return value;
        return string.Concat(value.AsSpan(0, Math.Max(maxLength - 1, 0)), "~");
    }

    private static List<string> SplitLines(string content) =>
        content.Replace("\r", string.Empty).Split('\n').ToList();

    private static bool ShouldQuit(Key key, int keyValue) =>
        keyValue is 'q' or 'Q' || keyValue == 3 ||
        key == (Key.CtrlMask | (Key)'c') ||
        key == (Key.CtrlMask | (Key)'C');
}
