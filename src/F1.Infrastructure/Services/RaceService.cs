using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.OpenF1;
using F1.Infrastructure.OpenF1.Models;

namespace F1.Infrastructure.Services;

public sealed class RaceService : IRaceService
{
    private readonly IOpenF1Client? openF1Client;

    public RaceService()
    {
    }

    public RaceService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
    }

    public Task<IReadOnlyList<Race>> GetRacesBySeasonAsync(
        int season,
        CancellationToken cancellationToken = default)
    {
        if (openF1Client is null)
        {
            var localRaces = BuildSampleRaces(season)
                .OrderBy(race => race.RoundNumber)
                .ToList();

            return Task.FromResult<IReadOnlyList<Race>>(localRaces);
        }

        return GetRacesFromApiAsync(season, cancellationToken);
    }

    private async Task<IReadOnlyList<Race>> GetRacesFromApiAsync(int season, CancellationToken cancellationToken)
    {
        try
        {
            var raceSessions = await openF1Client!.GetSessionsAsync(season, "Race", cancellationToken);
            var mappedRaces = MapRaces(raceSessions, season);
            if (mappedRaces.Count > 0)
            {
                return mappedRaces;
            }
        }
        catch
        {
        }

        return BuildSampleRaces(season)
            .OrderBy(race => race.RoundNumber)
            .ToList();
    }

    private static IReadOnlyList<Race> MapRaces(IReadOnlyList<OpenF1SessionDto> sessions, int season)
    {
        if (sessions.Count == 0)
        {
            return [];
        }

        var byMeeting = sessions
            .Where(session => (session.Year ?? season) == season)
            .GroupBy(session => session.MeetingKey)
            .Select(group =>
            {
                var representative = group.First();
                var raceName =
                    FirstNonEmpty(
                        representative.MeetingName,
                        representative.CountryName is null ? null : $"{representative.CountryName} Grand Prix",
                        representative.Location is null ? null : $"{representative.Location} Grand Prix")
                    ?? "Unknown Grand Prix";

                var date = ReadDateOnly(representative.DateStart);
                var round = representative.Round;

                return new
                {
                    representative.MeetingKey,
                    Name = raceName,
                    Date = date,
                    Round = round
                };
            })
            .OrderBy(entry => entry.Round ?? int.MaxValue)
            .ThenBy(entry => entry.Date ?? DateOnly.MinValue)
            .ThenBy(entry => entry.Name)
            .ToList();

        var races = new List<Race>(byMeeting.Count);
        for (var i = 0; i < byMeeting.Count; i++)
        {
            var entry = byMeeting[i];
            var roundNumber = entry.Round ?? i + 1;
            races.Add(new Race(season, roundNumber, entry.Name, entry.MeetingKey, entry.Date));
        }

        return races;
    }

    private static DateOnly? ReadDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(value, out var timestamp))
        {
            return null;
        }

        return DateOnly.FromDateTime(timestamp.UtcDateTime);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static IReadOnlyList<Race> BuildSampleRaces(int season)
    {
        return
        [
            new(season, 3, "Australian Grand Prix", null, new DateOnly(season, 3, 24)),
            new(season, 1, "Bahrain Grand Prix", null, new DateOnly(season, 3, 2)),
            new(season, 4, "Japanese Grand Prix", null, new DateOnly(season, 4, 7)),
            new(season, 2, "Saudi Arabian Grand Prix", null, new DateOnly(season, 3, 9))
        ];
    }
}
