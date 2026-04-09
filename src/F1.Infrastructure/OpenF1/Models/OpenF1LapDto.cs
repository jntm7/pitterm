using System.Text.Json.Serialization;

namespace F1.Infrastructure.OpenF1.Models;

public sealed class OpenF1LapDto
{
    [JsonPropertyName("driver_number")]
    public int? DriverNumber { get; init; }

    [JsonPropertyName("lap_number")]
    public int? LapNumber { get; init; }

    [JsonPropertyName("lap_duration")]
    public double? LapDuration { get; init; }

    [JsonPropertyName("date_start")]
    public string? DateStart { get; init; }
}
