using System.Text.Json.Serialization;

namespace F1.Infrastructure.OpenF1.Models;

public sealed class OpenF1PitDto
{
    [JsonPropertyName("driver_number")]
    public int? DriverNumber { get; init; }

    [JsonPropertyName("lap_number")]
    public int? LapNumber { get; init; }

    [JsonPropertyName("pit_duration")]
    public double? PitDuration { get; init; }

    [JsonPropertyName("date")]
    public string? Date { get; init; }
}
