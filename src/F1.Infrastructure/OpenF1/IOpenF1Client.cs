using F1.Infrastructure.OpenF1.Models;

namespace F1.Infrastructure.OpenF1;

public interface IOpenF1Client
{
    Task<IReadOnlyList<OpenF1SessionDto>> GetSessionsAsync(
        int year,
        string? sessionName = null,
        CancellationToken cancellationToken = default);
}
