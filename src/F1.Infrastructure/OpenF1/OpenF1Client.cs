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

    public async Task<IReadOnlyList<OpenF1DriverStandingDto>> GetDriverStandingsAsync(
        int year,
        int? meetingKey = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = "championship_drivers";
        if (meetingKey.HasValue)
        {
            endpoint = $"{endpoint}?meeting_key={meetingKey.Value}";
        }

        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var standings = await JsonSerializer.DeserializeAsync<List<OpenF1DriverStandingDto>>(
            stream,
            SerializerOptions,
            cancellationToken);

        return standings ?? [];
    }

    public async Task<IReadOnlyList<OpenF1ConstructorStandingDto>> GetConstructorStandingsAsync(
        int year,
        int? meetingKey = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = "championship_teams";
        if (meetingKey.HasValue)
        {
            endpoint = $"{endpoint}?meeting_key={meetingKey.Value}";
        }

        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var standings = await JsonSerializer.DeserializeAsync<List<OpenF1ConstructorStandingDto>>(
            stream,
            SerializerOptions,
            cancellationToken);

        return standings ?? [];
    }

    public async Task<IReadOnlyList<OpenF1PositionDto>> GetPositionsAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = "position";
        var filters = new List<string>();
        if (meetingKey.HasValue)
        {
            filters.Add($"meeting_key={meetingKey.Value}");
        }

        if (sessionKey.HasValue)
        {
            filters.Add($"session_key={sessionKey.Value}");
        }

        if (filters.Count > 0)
        {
            endpoint = $"{endpoint}?{string.Join("&", filters)}";
        }

        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var positions = await JsonSerializer.DeserializeAsync<List<OpenF1PositionDto>>(
            stream,
            SerializerOptions,
            cancellationToken);

        return positions ?? [];
    }

    public async Task<IReadOnlyList<OpenF1DriverDto>> GetDriversAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = "drivers";
        var filters = new List<string>();
        if (meetingKey.HasValue)
        {
            filters.Add($"meeting_key={meetingKey.Value}");
        }

        if (sessionKey.HasValue)
        {
            filters.Add($"session_key={sessionKey.Value}");
        }

        if (filters.Count > 0)
        {
            endpoint = $"{endpoint}?{string.Join("&", filters)}";
        }

        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var drivers = await JsonSerializer.DeserializeAsync<List<OpenF1DriverDto>>(
            stream,
            SerializerOptions,
            cancellationToken);

        return drivers ?? [];
    }

    public async Task<IReadOnlyList<OpenF1WeatherDto>> GetWeatherAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = BuildFilteredEndpoint("weather", meetingKey, sessionKey);
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var items = await JsonSerializer.DeserializeAsync<List<OpenF1WeatherDto>>(stream, SerializerOptions, cancellationToken);
        return items ?? [];
    }

    public async Task<IReadOnlyList<OpenF1PitDto>> GetPitStopsAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = BuildFilteredEndpoint("pit", meetingKey, sessionKey);
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var items = await JsonSerializer.DeserializeAsync<List<OpenF1PitDto>>(stream, SerializerOptions, cancellationToken);
        return items ?? [];
    }

    public async Task<IReadOnlyList<OpenF1LapDto>> GetLapsAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var endpoint = BuildFilteredEndpoint("laps", meetingKey, sessionKey);
        using var response = await httpClient.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var items = await JsonSerializer.DeserializeAsync<List<OpenF1LapDto>>(stream, SerializerOptions, cancellationToken);
        return items ?? [];
    }

    private static string BuildFilteredEndpoint(string resource, int? meetingKey, int? sessionKey)
    {
        var filters = new List<string>();
        if (meetingKey.HasValue)
        {
            filters.Add($"meeting_key={meetingKey.Value}");
        }

        if (sessionKey.HasValue)
        {
            filters.Add($"session_key={sessionKey.Value}");
        }

        if (filters.Count == 0)
        {
            return resource;
        }

        return $"{resource}?{string.Join("&", filters)}";
    }
}
