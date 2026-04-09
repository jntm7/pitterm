using F1.Core.Models;

namespace F1.Core.Services;

public interface IDriverStandingsService
{
    Task<IReadOnlyList<DriverStanding>> GetDriverStandingsAsync(
        int season,
        int? meetingKey = null,
        CancellationToken cancellationToken = default);
}
