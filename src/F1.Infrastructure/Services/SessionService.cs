using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.OpenF1;
using F1.Infrastructure.OpenF1.Models;

namespace F1.Infrastructure.Services;

public sealed class SessionService : ISessionService
{
    private readonly IOpenF1Client? openF1Client;

    public SessionService()
    {
    }

    public SessionService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
    }

    public Task<IReadOnlyList<Session>> GetSessionsByRaceAsync(
        int season,
        int roundNumber,
        int? meetingKey = null,
        CancellationToken cancellationToken = default)
    {
        if (openF1Client is null)
        {
            return Task.FromResult<IReadOnlyList<Session>>(BuildFallbackSessions(season, roundNumber));
        }

        return GetSessionsFromApiAsync(season, roundNumber, meetingKey, cancellationToken);
    }

    private async Task<IReadOnlyList<Session>> GetSessionsFromApiAsync(
        int season,
        int roundNumber,
        int? meetingKey,
        CancellationToken cancellationToken)
    {
        try
        {
            var openF1Sessions = await openF1Client!.GetSessionsAsync(season, null, cancellationToken);
            var sessions = MapSessions(openF1Sessions, season, roundNumber, meetingKey);
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

    private static IReadOnlyList<Session> MapSessions(
        IReadOnlyList<OpenF1SessionDto> sessions,
        int season,
        int roundNumber,
        int? meetingKey)
    {
        if (sessions.Count == 0)
        {
            return [];
        }

        var mappedSessions = new List<Session>();
        foreach (var item in sessions)
        {
            var sessionName = item.SessionName;
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                continue;
            }

            var sessionSeason = item.Year ?? season;
            if (sessionSeason != season)
            {
                continue;
            }

            var detectedRound = item.Round;
            if (detectedRound.HasValue && detectedRound.Value != roundNumber)
            {
                continue;
            }

            if (meetingKey.HasValue && item.MeetingKey.HasValue && item.MeetingKey.Value != meetingKey.Value)
            {
                continue;
            }

            var start = ReadDateTime(item.DateStart);
            var end = ReadDateTime(item.DateEnd);
            mappedSessions.Add(new Session(
                season,
                detectedRound ?? roundNumber,
                sessionName,
                item.MeetingKey,
                item.SessionKey,
                start,
                end));
        }

        return mappedSessions
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

    private static DateTimeOffset? ReadDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, out var parsed)
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
