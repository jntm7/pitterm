namespace F1.Core.Models;

public sealed record Session(
    int Season,
    int RoundNumber,
    string SessionName,
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null);
