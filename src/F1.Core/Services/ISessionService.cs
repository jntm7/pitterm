using F1.Core.Models;

namespace F1.Core.Services;

public interface ISessionService
{
    Task<IReadOnlyList<Session>> GetSessionsByRaceAsync(
        int season,
        int roundNumber,
        int? meetingKey = null,
        CancellationToken cancellationToken = default);
}
