using System.Text.Json.Serialization;

namespace F1.Infrastructure.Jolpica;

public sealed class JolpicaDriverDto
{
    [JsonPropertyName("driverId")]
    public string? DriverId { get; init; }

    [JsonPropertyName("permanentNumber")]
    public string? PermanentNumber { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("givenName")]
    public string? GivenName { get; init; }

    [JsonPropertyName("familyName")]
    public string? FamilyName { get; init; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; init; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; init; }
}
