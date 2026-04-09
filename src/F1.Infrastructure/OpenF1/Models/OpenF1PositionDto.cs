using System.Text.Json.Serialization;

namespace F1.Infrastructure.OpenF1.Models;

public sealed class OpenF1PositionDto
{
    [JsonPropertyName("driver_number")]
    public int? DriverNumber { get; init; }

    [JsonPropertyName("position")]
    public int? Position { get; init; }

    [JsonPropertyName("meeting_key")]
    public int? MeetingKey { get; init; }

    [JsonPropertyName("session_key")]
    public int? SessionKey { get; init; }

    [JsonPropertyName("date")]
    public string? Date { get; init; }
}
