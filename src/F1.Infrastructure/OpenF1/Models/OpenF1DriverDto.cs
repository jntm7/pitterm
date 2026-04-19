using System.Text.Json.Serialization;

namespace F1.Infrastructure.OpenF1.Models;

public sealed class OpenF1DriverDto
{
    [JsonPropertyName("driver_number")]
    public int? DriverNumber { get; init; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; init; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }

    [JsonPropertyName("name_acronym")]
    public string? NameAcronym { get; init; }

    [JsonPropertyName("team_name")]
    public string? TeamName { get; init; }

    [JsonPropertyName("team_colour")]
    public string? TeamColour { get; init; }

    [JsonPropertyName("meeting_key")]
    public int? MeetingKey { get; init; }

    [JsonPropertyName("session_key")]
    public int? SessionKey { get; init; }
}