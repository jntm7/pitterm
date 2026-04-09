namespace F1.Core.Models;

public sealed record Session(
    int Season,
    int RoundNumber,
    string SessionName,
    int? MeetingKey = null,
    int? SessionKey = null,
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null);
