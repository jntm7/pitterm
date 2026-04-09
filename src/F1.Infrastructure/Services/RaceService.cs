using F1.Core.Models;
using F1.Core.Services;
using System.Text.Json;

namespace F1.Infrastructure.Services;

public sealed class RaceService : IRaceService
{
    private readonly HttpClient? httpClient;

    public RaceService()
    {
    }

    public RaceService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public Task<IReadOnlyList<Race>> GetRacesBySeasonAsync(
        int season,
        CancellationToken cancellationToken = default)
    {
        if (httpClient is null)
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
            using var response = await httpClient!.GetAsync($"sessions?year={season}&session_name=Race", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return BuildSampleRaces(season)
                    .OrderBy(race => race.RoundNumber)
                    .ToList();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var mappedRaces = MapRaces(document.RootElement, season);
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

    private static IReadOnlyList<Race> MapRaces(JsonElement root, int season)
    {
        if (root.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var candidates = new List<(string Name, DateOnly? Date)>();

        foreach (var item in root.EnumerateArray())
        {
            var meetingName = ReadString(item, "meeting_name");
            var countryName = ReadString(item, "country_name");
            var location = ReadString(item, "location");

            var raceName =
                FirstNonEmpty(meetingName, countryName is null ? null : $"{countryName} Grand Prix", location is null ? null : $"{location} Grand Prix")
                ?? "Unknown Grand Prix";

            var date = ReadDateOnly(item, "date_start");
            candidates.Add((raceName, date));
        }

        var ordered = candidates
            .OrderBy(candidate => candidate.Date ?? DateOnly.MinValue)
            .ThenBy(candidate => candidate.Name)
            .ToList();

        var races = new List<Race>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var candidate = ordered[i];
            races.Add(new Race(season, i + 1, candidate.Name, candidate.Date));
        }

        return races;
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

    private static DateOnly? ReadDateOnly(JsonElement element, string propertyName)
    {
        var text = ReadString(element, propertyName);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(text, out var timestamp))
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
            new(season, 3, "Australian Grand Prix", new DateOnly(season, 3, 24)),
            new(season, 1, "Bahrain Grand Prix", new DateOnly(season, 3, 2)),
            new(season, 4, "Japanese Grand Prix", new DateOnly(season, 4, 7)),
            new(season, 2, "Saudi Arabian Grand Prix", new DateOnly(season, 3, 9))
        ];
    }
}
