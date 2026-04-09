using System.Text.Json.Serialization;

namespace F1.Infrastructure.OpenF1.Models;

public sealed class OpenF1ConstructorStandingDto
{
    [JsonPropertyName("team_name")]
    public string? TeamName { get; init; }

    [JsonPropertyName("meeting_key")]
    public int? MeetingKey { get; init; }

    [JsonPropertyName("session_key")]
    public int? SessionKey { get; init; }

    [JsonPropertyName("position_current")]
    public int? PositionCurrent { get; init; }

    [JsonPropertyName("points_current")]
    public double? PointsCurrent { get; init; }
}
