using F1.Core.Models;
using F1.Core.Services;

namespace F1.Infrastructure.Services;

public sealed class SeasonService : ISeasonService
{
    public Task<IReadOnlyList<Season>> GetSeasonsAsync(CancellationToken cancellationToken = default)
    {
        var seasons = Enumerable.Range(2018, 9)
            .OrderByDescending(year => year)
            .Select(year => new Season(year))
            .ToList();

        return Task.FromResult<IReadOnlyList<Season>>(seasons);
    }
}
