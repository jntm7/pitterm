using F1.Core.Models;

namespace F1.Core.Services;

public interface IRaceResultsService
{
    Task<IReadOnlyList<RaceResult>> GetRaceResultsAsync(
        int season,
        int? meetingKey,
        int? sessionKey,
        CancellationToken cancellationToken = default);
}
