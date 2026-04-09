using F1.Infrastructure.OpenF1.Models;
using System.Text.Json;

namespace F1.Infrastructure.OpenF1;

public sealed class OpenF1Client : IOpenF1Client
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;

    public OpenF1Client(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyList<OpenF1SessionDto>> GetSessionsAsync(
        int year,
        string? sessionName = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"sessions?year={year}";
        if (!string.IsNullOrWhiteSpace(sessionName))
        {
            endpoint = $"{endpoint}&session_name={Uri.EscapeDataString(sessionName)}";
        }

        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var sessions = await JsonSerializer.DeserializeAsync<List<OpenF1SessionDto>>(
            stream,
            SerializerOptions,
            cancellationToken);

        return sessions ?? [];
    }
}
