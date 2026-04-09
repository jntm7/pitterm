using F1.Core.State;

namespace F1Tui.State;

public static class AppStateTransitions
{
    public static AppState ToSeasons(AppState current, int? selectedSeason, string statusMessage)
    {
        return current with
        {
            SelectedSeason = selectedSeason,
            SelectedRoundNumber = null,
            SelectedGrandPrixName = null,
            SelectedSessionName = null,
            ActiveScreen = "Seasons",
            StatusMessage = statusMessage
        };
    }

    public static AppState ToRaces(AppState current, int season, string? grandPrixLabel, string statusMessage)
    {
        return current with
        {
            SelectedSeason = season,
            SelectedRoundNumber = null,
            SelectedGrandPrixName = grandPrixLabel,
            SelectedSessionName = null,
            ActiveScreen = "Races",
            StatusMessage = statusMessage
        };
    }

    public static AppState ToSessions(AppState current, int? roundNumber, string? grandPrixName, string statusMessage)
    {
        return current with
        {
            SelectedRoundNumber = roundNumber,
            SelectedGrandPrixName = grandPrixName,
            SelectedSessionName = null,
            ActiveScreen = "Sessions",
            StatusMessage = statusMessage
        };
    }

    public static AppState ToSessionDetail(AppState current, string sessionName, string statusMessage)
    {
        return current with
        {
            SelectedSessionName = sessionName,
            ActiveScreen = "SessionDetail",
            StatusMessage = statusMessage
        };
    }
}
