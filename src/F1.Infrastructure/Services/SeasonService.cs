using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.OpenF1;

namespace F1.Infrastructure.Services;

public sealed class SeasonService : ISeasonService
{
    private readonly IOpenF1Client? openF1Client;

    public SeasonService()
    {
    }

    public SeasonService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
    }

    public async Task<IReadOnlyList<Season>> GetSeasonsAsync(CancellationToken cancellationToken = default)
    {
        if (openF1Client is null)
        {
            return BuildFallbackSeasons();
        }

        try
        {
            var currentYear = DateTime.UtcNow.Year;
            var years = new List<int>();

            for (var year = 2023; year <= currentYear + 1; year++)
            {
                try
                {
                    var sessions = await openF1Client.GetSessionsAsync(year, null, cancellationToken);
                    if (sessions.Count > 0)
                    {
                        years.Add(year);
                    }
                }
                catch
                {
                    continue;
                }
            }

            if (years.Count > 0)
            {
                var baselineYears = Enumerable.Range(2023, Math.Max(currentYear - 2023 + 1, 1));

                return years
                    .Union(baselineYears)
                    .OrderByDescending(year => year)
                    .Select(year => new Season(year))
                    .ToList();
            }
        }
        catch
        {
        }

        return BuildFallbackSeasons();
    }

    private static IReadOnlyList<Season> BuildFallbackSeasons()
    {
        var currentYear = DateTime.UtcNow.Year;
        return Enumerable.Range(2023, Math.Max(currentYear - 2023 + 1, 1))
            .OrderByDescending(year => year)
            .Select(year => new Season(year))
            .ToList();
    }
}
