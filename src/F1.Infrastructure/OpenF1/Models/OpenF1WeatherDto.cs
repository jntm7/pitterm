using System.Text.Json.Serialization;

namespace F1.Infrastructure.OpenF1.Models;

public sealed class OpenF1WeatherDto
{
    [JsonPropertyName("date")]
    public string? Date { get; init; }

    [JsonPropertyName("air_temperature")]
    public double? AirTemperature { get; init; }

    [JsonPropertyName("track_temperature")]
    public double? TrackTemperature { get; init; }

    [JsonPropertyName("rainfall")]
    public double? Rainfall { get; init; }
}
