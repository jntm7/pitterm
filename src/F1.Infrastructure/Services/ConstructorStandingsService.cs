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
            return [];
        }

        try
        {
            var dtos = await openF1Client.GetConstructorStandingsAsync(season, meetingKey, cancellationToken);
            if (dtos.Count == 0 && meetingKey.HasValue)
            {
                dtos = await openF1Client.GetConstructorStandingsAsync(season, null, cancellationToken);
            }

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

            if (standings.Count >= 10)
            {
                return standings;
            }

            var derived = await BuildFromDriverStandingsAsync(season, meetingKey, cancellationToken);
            if (derived.Count > 0)
            {
                return derived;
            }

            if (standings.Count > 0)
            {
                return standings;
            }
        }
        catch
        {
        }

        return [];
    }

    private async Task<IReadOnlyList<ConstructorStanding>> BuildFromDriverStandingsAsync(
        int season,
        int? meetingKey,
        CancellationToken cancellationToken)
    {
        if (openF1Client is null)
        {
            return [];
        }

        var driverStandings = await openF1Client.GetDriverStandingsAsync(season, meetingKey, cancellationToken);
        if (driverStandings.Count == 0)
        {
            return [];
        }

        var drivers = await openF1Client.GetDriversAsync(meetingKey, null, cancellationToken);
        if (drivers.Count == 0)
        {
            drivers = await openF1Client.GetDriversAsync(null, null, cancellationToken);
        }

        var driverToTeam = drivers
            .Where(driver => driver.DriverNumber.HasValue && !string.IsNullOrWhiteSpace(driver.TeamName))
            .GroupBy(driver => driver.DriverNumber!.Value)
            .ToDictionary(group => group.Key, group => group.First().TeamName!);

        var teamPoints = driverStandings
            .Where(item => item.DriverNumber.HasValue)
            .Select(item => new
            {
                Team = driverToTeam.TryGetValue(item.DriverNumber!.Value, out var teamName)
                    ? teamName
                    : "Unknown Team",
                Points = item.PointsCurrent ?? 0
            })
            .Where(item => !string.Equals(item.Team, "Unknown Team", StringComparison.OrdinalIgnoreCase))
            .GroupBy(item => item.Team, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                TeamName = group.Key,
                Points = group.Sum(item => item.Points)
            })
            .OrderByDescending(item => item.Points)
            .ThenBy(item => item.TeamName)
            .ToList();

        var standings = new List<ConstructorStanding>(teamPoints.Count);
        for (var i = 0; i < teamPoints.Count; i++)
        {
            standings.Add(new ConstructorStanding(i + 1, teamPoints[i].TeamName, teamPoints[i].Points));
        }

        return standings;
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

}
