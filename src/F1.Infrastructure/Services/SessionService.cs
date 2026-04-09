using F1.Core.Models;
using F1.Core.Services;

namespace F1.Infrastructure.Services;

public sealed class SessionService : ISessionService
{
    public Task<IReadOnlyList<Session>> GetSessionsByRaceAsync(
        int season,
        int roundNumber,
        CancellationToken cancellationToken = default)
    {
        var sessions = new List<Session>
        {
            new(season, roundNumber, "Practice 1"),
            new(season, roundNumber, "Practice 2"),
            new(season, roundNumber, "Practice 3"),
            new(season, roundNumber, "Qualifying"),
            new(season, roundNumber, "Race")
        };

        return Task.FromResult<IReadOnlyList<Session>>(sessions);
    }
}
