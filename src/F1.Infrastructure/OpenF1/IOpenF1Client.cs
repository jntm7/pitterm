using F1.Infrastructure.OpenF1.Models;

namespace F1.Infrastructure.OpenF1;

public interface IOpenF1Client
{
    Task<IReadOnlyList<OpenF1SessionDto>> GetSessionsAsync(
        int year,
        string? sessionName = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenF1DriverStandingDto>> GetDriverStandingsAsync(
        int year,
        int? meetingKey = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenF1ConstructorStandingDto>> GetConstructorStandingsAsync(
        int year,
        int? meetingKey = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenF1PositionDto>> GetPositionsAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OpenF1DriverDto>> GetDriversAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default);
}
