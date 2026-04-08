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
                SelectedGrandPrixName = null,
                StatusMessage = BuildStatusMessage(initialSeason, null)
            });
        }

        logger.LogInformation(
            "Initial app state: screen={Screen}, season={Season}, grandPrix={GrandPrix}",
            stateStore.Current.ActiveScreen,
            stateStore.Current.SelectedSeason,
            stateStore.Current.SelectedGrandPrixName ?? "n/a");

        var seasonNames = seasons.Select(season => season.Year.ToString()).ToList();

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
            Height = Dim.Fill() - 3
        };
        listView.ColorScheme = darkScheme;

        var statusLine = new Label(stateStore.Current.StatusMessage ?? "Ready")
        {
            X = 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill() - 2,
            Height = 1
        };
        statusLine.ColorScheme = darkScheme;

        var shortcutsLine = new Label("[Q] Quit")
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill() - 2,
            Height = 1
        };
        shortcutsLine.ColorScheme = darkScheme;

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

        listView.KeyPress += args =>
        {
            if (ShouldQuit(args.KeyEvent.Key, args.KeyEvent.KeyValue))
            {
                args.Handled = true;
                Application.RequestStop();
            }
        };

        listView.SelectedItemChanged += args =>
        {
            var selectedSeason = seasons[args.Item].Year;

            stateStore.Update(state => state with
            {
                SelectedSeason = selectedSeason,
                SelectedGrandPrixName = null,
                StatusMessage = BuildStatusMessage(selectedSeason, null)
            });

            statusLine.Text = stateStore.Current.StatusMessage ?? "Ready";
        };

        window.Add(title, listView, statusLine, shortcutsLine);
        top.Add(window);

        Application.Run();
        Application.Shutdown();
    }

    private string BuildStatusMessage(int? selectedSeason, string? selectedGrandPrixName)
    {
        var grandPrixName = string.IsNullOrWhiteSpace(selectedGrandPrixName)
            ? "n/a"
            : selectedGrandPrixName;

        return $"Season {selectedSeason?.ToString() ?? "none"} | Grand Prix: {grandPrixName} |  Data: {options.Value.ApiBaseUrl}";
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
