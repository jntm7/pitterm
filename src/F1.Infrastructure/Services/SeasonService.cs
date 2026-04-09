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
                    break;
                }
            }

            if (years.Count > 0)
            {
                return years
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
        return
        [
            new(2025),
            new(2024),
            new(2023)
        ];
    }
}
