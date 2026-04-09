using F1.Core.Models;

namespace F1.Core.Services;

public interface IConstructorStandingsService
{
    Task<IReadOnlyList<ConstructorStanding>> GetConstructorStandingsAsync(
        int season,
        int? meetingKey = null,
        CancellationToken cancellationToken = default);
}
