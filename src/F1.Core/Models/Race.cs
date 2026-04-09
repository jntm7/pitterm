namespace F1.Core.Models;

public sealed record Race(
    int Season,
    int RoundNumber,
    string GrandPrixName,
    string? Location,
    int? MeetingKey = null,
    DateOnly? RaceDate = null);
