using System.Text.Json.Serialization;

namespace F1.Infrastructure.OpenF1.Models;

public sealed class OpenF1SessionDto
{
    [JsonPropertyName("session_key")]
    public int? SessionKey { get; init; }

    [JsonPropertyName("meeting_key")]
    public int? MeetingKey { get; init; }

    [JsonPropertyName("session_name")]
    public string? SessionName { get; init; }

    [JsonPropertyName("meeting_name")]
    public string? MeetingName { get; init; }

    [JsonPropertyName("country_name")]
    public string? CountryName { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("year")]
    public int? Year { get; init; }

    [JsonPropertyName("round")]
    public int? Round { get; init; }

    [JsonPropertyName("date_start")]
    public string? DateStart { get; init; }

    [JsonPropertyName("date_end")]
    public string? DateEnd { get; init; }
}
