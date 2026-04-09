namespace F1.Core.Models;

public sealed record WeatherSample(
    DateTimeOffset? Timestamp,
    double? AirTemperature,
    double? TrackTemperature,
    double? Rainfall);
