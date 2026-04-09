using F1.Core.Models;
using F1.Core.Services;
using System.Text.Json;

namespace F1.Infrastructure.Services;

public sealed class SessionService : ISessionService
{
    private readonly HttpClient? httpClient;

    public SessionService()
    {
    }

    public SessionService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<Session>> GetSessionsByRaceAsync(
        int season,
        int roundNumber,
        CancellationToken cancellationToken = default)
    {
        if (httpClient is null)
        {
            return Task.FromResult<IReadOnlyList<Session>>(BuildFallbackSessions(season, roundNumber));
        }

        return GetSessionsFromApiAsync(season, roundNumber, cancellationToken);
    }

    private async Task<IReadOnlyList<Session>> GetSessionsFromApiAsync(
        int season,
        int roundNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient!.GetAsync($"sessions?year={season}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return BuildFallbackSessions(season, roundNumber);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var sessions = MapSessions(document.RootElement, season, roundNumber);
            if (sessions.Count > 0)
            {
                return sessions;
            }
        }
        catch
        {
        }

        return BuildFallbackSessions(season, roundNumber);
    }

    private static IReadOnlyList<Session> MapSessions(JsonElement root, int season, int roundNumber)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var sessions = new List<Session>();
        foreach (var item in root.EnumerateArray())
        {
            var sessionName = ReadString(item, "session_name");
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                continue;
            }

            var sessionSeason = ReadInt(item, "year") ?? season;
            if (sessionSeason != season)
            {
                continue;
            }

            var detectedRound = ReadInt(item, "round");
            if (detectedRound.HasValue && detectedRound.Value != roundNumber)
            {
                continue;
            }

            var start = ReadDateTime(item, "date_start");
            var end = ReadDateTime(item, "date_end");
            sessions.Add(new Session(season, roundNumber, sessionName, start, end));
        }

        return sessions
            .OrderBy(session => SessionOrder(session.SessionName))
            .ThenBy(session => session.StartTime ?? DateTimeOffset.MinValue)
            .ToList();
    }

    private static IReadOnlyList<Session> BuildFallbackSessions(int season, int roundNumber)
    {
        return
        [
            new(season, roundNumber, "Practice 1"),
            new(season, roundNumber, "Practice 2"),
            new(season, roundNumber, "Practice 3"),
            new(season, roundNumber, "Qualifying"),
            new(season, roundNumber, "Race")
        ];
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numericValue))
        {
            return numericValue;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static DateTimeOffset? ReadDateTime(JsonElement element, string propertyName)
    {
        var text = ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return DateTimeOffset.TryParse(text, out var parsed)
            ? parsed
            : null;
    }

    private static int SessionOrder(string sessionName)
    {
        var normalized = sessionName.Trim().ToLowerInvariant();

        if (normalized.Contains("practice 1") || normalized == "fp1")
        {
            return 10;
        }

        if (normalized.Contains("practice 2") || normalized == "fp2")
        {
            return 20;
        }

        if (normalized.Contains("practice 3") || normalized == "fp3")
        {
            return 30;
        }

        if (normalized.Contains("sprint shootout"))
        {
            return 35;
        }

        if (normalized == "sprint" || normalized.Contains("sprint race"))
        {
            return 40;
        }

        if (normalized.Contains("qualifying"))
        {
            return 50;
        }

        if (normalized == "race" || normalized.Contains("grand prix"))
        {
            return 60;
        }

        return 99;
    }
}
