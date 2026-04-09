namespace F1.Core.Models;

public sealed record PitStop(
    int? DriverNumber,
    string? DriverName,
    string? TeamName,
    int? LapNumber,
    double? PitDuration,
    DateTimeOffset? Timestamp);
