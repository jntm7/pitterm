namespace F1.Core.State;

public sealed record AppState
{
    public int? SelectedSeason { get; init; }
    public int? SelectedRoundNumber { get; init; }
    public string? SelectedGrandPrixName { get; init; }
    public string? SelectedSessionName { get; init; }
    public string ActiveScreen { get; init; } = "Seasons";
    public string? StatusMessage { get; init; }
}
