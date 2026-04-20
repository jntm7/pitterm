using System.Text.Json.Serialization;

namespace F1.Infrastructure.Jolpica;

public sealed class JolpicaDriverResponseDto
{
    [JsonPropertyName("MRData")]
    public JolpicaMrDataDto? MrData { get; init; }
}

public sealed class JolpicaMrDataDto
{
    [JsonPropertyName("DriverTable")]
    public JolpicaDriverTableDto? DriverTable { get; init; }
}

public sealed class JolpicaDriverTableDto
{
    [JsonPropertyName("season")]
    public string? Season { get; init; }

    [JsonPropertyName("Drivers")]
    public List<JolpicaDriverDto>? Drivers { get; init; }
}
