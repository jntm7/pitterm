using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.OpenF1;
using F1.Infrastructure.OpenF1.Models;

namespace F1.Infrastructure.Services;

public sealed class ConstructorStandingsService : IConstructorStandingsService
{
    private readonly IOpenF1Client? openF1Client;

    public ConstructorStandingsService()
    {
    }

    public ConstructorStandingsService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
    }

    public async Task<IReadOnlyList<ConstructorStanding>> GetConstructorStandingsAsync(
        int season,
        int? meetingKey = null,
        CancellationToken cancellationToken = default)
    {
        if (openF1Client is null)
        {
            return BuildFallback();
        }

        try
        {
            var dtos = await openF1Client.GetConstructorStandingsAsync(season, meetingKey, cancellationToken);
            var scopedDtos = ScopeByMeeting(dtos, meetingKey);

            var dedupedDtos = scopedDtos
                .Where(dto => !string.IsNullOrWhiteSpace(dto.TeamName))
                .GroupBy(dto => dto.TeamName!, StringComparer.OrdinalIgnoreCase)
                .Select(group => group
                    .OrderByDescending(dto => dto.SessionKey ?? int.MinValue)
                    .ThenByDescending(dto => dto.PointsCurrent ?? double.MinValue)
                    .ThenBy(dto => dto.PositionCurrent ?? int.MaxValue)
                    .First())
                .ToList();

            var standings = dedupedDtos
                .Where(dto => !string.IsNullOrWhiteSpace(dto.TeamName))
                .Select(dto => new ConstructorStanding(
                    dto.PositionCurrent ?? int.MaxValue,
                    dto.TeamName ?? "Unknown Team",
                    dto.PointsCurrent ?? 0))
                .OrderBy(dto => dto.Position)
                .ThenByDescending(dto => dto.Points)
                .ToList();

            if (standings.Count > 0)
            {
                return standings;
            }
        }
        catch
        {
        }

        return BuildFallback();
    }

    private static IReadOnlyList<OpenF1ConstructorStandingDto> ScopeByMeeting(
        IReadOnlyList<OpenF1ConstructorStandingDto> dtos,
        int? meetingKey)
    {
        if (!meetingKey.HasValue)
        {
            return dtos;
        }

        var exact = dtos.Where(dto => dto.MeetingKey == meetingKey.Value).ToList();
        if (exact.Count > 0)
        {
            return exact;
        }

        var upToMeeting = dtos
            .Where(dto => dto.MeetingKey.HasValue && dto.MeetingKey.Value <= meetingKey.Value)
            .ToList();

        if (upToMeeting.Count == 0)
        {
            return dtos;
        }

        var latestMeeting = upToMeeting
            .Where(dto => dto.MeetingKey.HasValue)
            .Max(dto => dto.MeetingKey!.Value);

        return upToMeeting
            .Where(dto => dto.MeetingKey == latestMeeting)
            .ToList();
    }

    private static IReadOnlyList<ConstructorStanding> BuildFallback()
    {
        return
        [
            new(1, "Red Bull Racing", 620),
            new(2, "Ferrari", 560),
            new(3, "McLaren", 540),
            new(4, "Mercedes", 420),
            new(5, "Aston Martin", 210),
            new(6, "RB", 136),
            new(7, "Haas", 124),
            new(8, "Williams", 79),
            new(9, "Alpine", 72),
            new(10, "Kick Sauber", 39)
        ];
    }
}
