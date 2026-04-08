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
    private readonly IAppStateStore stateStore;
    private readonly IOptions<AppOptions> options;
    private readonly ILogger<TerminalApp> logger;

    public TerminalApp(
        ISeasonService seasonService,
        IAppStateStore stateStore,
        IOptions<AppOptions> options,
        ILogger<TerminalApp> logger)
    {
        this.seasonService = seasonService;
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

        var seasons = await seasonService.GetSeasonsAsync(cancellationToken);
        var initialSeason = seasons.FirstOrDefault()?.Year;

        if (initialSeason is not null)
        {
            stateStore.Update(state => state with
            {
                SelectedSeason = initialSeason,
                StatusMessage = BuildStatusMessage(seasons.Count, initialSeason)
            });
        }

        logger.LogInformation(
            "Initial app state: screen={Screen}, season={Season}, round={Round}",
            stateStore.Current.ActiveScreen,
            stateStore.Current.SelectedSeason,
            stateStore.Current.SelectedRound);

        var seasonNames = seasons.Select(season => season.Year.ToString()).ToList();

        Application.Init();

        var top = Application.Top;
        var menu = new MenuBar(
            new MenuBarItem[]
            {
                new("_File", new MenuItem[]
                {
                    new("_Quit", string.Empty, () => Application.RequestStop())
                })
            });

        var window = new Window("PitTerm")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var title = new Label("F1 Seasons")
        {
            X = 1,
            Y = 1
        };

        var listView = new ListView(seasonNames)
        {
            X = 1,
            Y = Pos.Bottom(title) + 1,
            Width = 30,
            Height = Dim.Fill() - 2
        };

        var statusBar = new Label(stateStore.Current.StatusMessage ?? "Ready")
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill() - 2,
            Height = 1
        };

        listView.SelectedItemChanged += args =>
        {
            var selectedSeason = seasons[args.Item].Year;

            stateStore.Update(state => state with
            {
                SelectedSeason = selectedSeason,
                StatusMessage = BuildStatusMessage(seasons.Count, selectedSeason)
            });

            statusBar.Text = stateStore.Current.StatusMessage ?? "Ready";
        };

        window.Add(title, listView, statusBar);
        top.Add(menu, window);

        Application.Run();
        Application.Shutdown();
    }

    private string BuildStatusMessage(int seasonCount, int? selectedSeason)
    {
        return $"Loaded {seasonCount} seasons | Selected {selectedSeason?.ToString() ?? "none"} | API {options.Value.ApiBaseUrl} | Timeout {options.Value.RequestTimeoutSeconds}s | Cache {options.Value.CacheTtlMinutes}m";
    }
}
