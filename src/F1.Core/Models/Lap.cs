namespace F1.Core.Models;

public sealed record Lap(
    int? DriverNumber,
    int? LapNumber,
    double? LapDuration,
    DateTimeOffset? Timestamp);
