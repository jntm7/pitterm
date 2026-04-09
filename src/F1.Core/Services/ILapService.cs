using F1.Core.Models;

namespace F1.Core.Services;

public interface ILapService
{
    Task<IReadOnlyList<Lap>> GetLapsAsync(
        int? meetingKey,
        int? sessionKey,
        CancellationToken cancellationToken = default);
}
