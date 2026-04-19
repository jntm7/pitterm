using F1.Core.Models;

namespace F1.Core.Services;

public interface IDriverService
{
    Task<IReadOnlyList<Driver>> GetDriversAsync(
        int? meetingKey = null,
        CancellationToken cancellationToken = default);
}