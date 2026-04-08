using F1.Core.Models;

namespace F1.Core.Services;

public interface ISeasonService
{
    Task<IReadOnlyList<Season>> GetSeasonsAsync(CancellationToken cancellationToken = default);
}
