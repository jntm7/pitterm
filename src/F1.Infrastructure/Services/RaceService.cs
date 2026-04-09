using F1.Core.Models;
using F1.Core.Services;

namespace F1.Infrastructure.Services;

public sealed class RaceService : IRaceService
{
    public Task<IReadOnlyList<Race>> GetRacesBySeasonAsync(
        int season,
        CancellationToken cancellationToken = default)
    {
        var races = BuildSampleRaces(season)
            .OrderBy(race => race.RoundNumber)
            .ToList();

        return Task.FromResult<IReadOnlyList<Race>>(races);
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
