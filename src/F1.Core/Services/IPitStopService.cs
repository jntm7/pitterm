using F1.Core.Models;

namespace F1.Core.Services;

public interface IPitStopService
{
    Task<IReadOnlyList<PitStop>> GetPitStopsAsync(
        int? meetingKey,
        int? sessionKey,
        CancellationToken cancellationToken = default);
}
