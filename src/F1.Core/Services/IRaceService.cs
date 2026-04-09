using F1.Core.Models;

namespace F1.Core.Services;

public interface IRaceService
{
    Task<IReadOnlyList<Race>> GetRacesBySeasonAsync(
        int season,
        CancellationToken cancellationToken = default);
}
