namespace F1.Core.Models;

public sealed record DriverStanding(
    int Position,
    string DriverName,
    string TeamName,
    double Points);
