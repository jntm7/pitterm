using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.OpenF1;
using F1.Infrastructure.OpenF1.Models;

namespace F1.Infrastructure.Services;

public sealed class DriverStandingsService : IDriverStandingsService
{
    private readonly IOpenF1Client? openF1Client;

    public DriverStandingsService()
    {
    }

    public DriverStandingsService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
    }

    public async Task<IReadOnlyList<DriverStanding>> GetDriverStandingsAsync(
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
            var dtos = await openF1Client.GetDriverStandingsAsync(season, meetingKey, cancellationToken);
            var scopedDtos = ScopeByMeeting(dtos, meetingKey);
            var drivers = await openF1Client.GetDriversAsync(meetingKey, null, cancellationToken);
            var driverLookup = drivers
                .Where(driver => driver.DriverNumber.HasValue)
                .GroupBy(driver => driver.DriverNumber!.Value)
                .ToDictionary(group => group.Key, group => group.First());

            var dedupedDtos = scopedDtos
                .Where(dto => dto.DriverNumber.HasValue)
                .GroupBy(dto => dto.DriverNumber!.Value)
                .Select(group => group
                    .OrderByDescending(dto => dto.SessionKey ?? int.MinValue)
                    .ThenByDescending(dto => dto.PointsCurrent ?? double.MinValue)
                    .ThenBy(dto => dto.PositionCurrent ?? int.MaxValue)
                    .First())
                .ToList();

            var standings = dedupedDtos
                .Where(dto => dto.DriverNumber.HasValue)
                .Select(dto => new DriverStanding(
                    dto.PositionCurrent ?? int.MaxValue,
                    ResolveDriverName(dto.DriverNumber, driverLookup),
                    ResolveTeamName(dto.DriverNumber, driverLookup),
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

    private static IReadOnlyList<OpenF1DriverStandingDto> ScopeByMeeting(
        IReadOnlyList<OpenF1DriverStandingDto> dtos,
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

    private static string ResolveDriverName(
        int? driverNumber,
        IReadOnlyDictionary<int, OpenF1DriverDto> driverLookup)
    {
        if (!driverNumber.HasValue)
        {
            return "Unknown Driver";
        }

        if (!driverLookup.TryGetValue(driverNumber.Value, out var driver))
        {
            return $"Driver {driverNumber.Value}";
        }

        return driver.FullName
            ?? driver.LastName
            ?? $"Driver {driverNumber.Value}";
    }

    private static string ResolveTeamName(
        int? driverNumber,
        IReadOnlyDictionary<int, OpenF1DriverDto> driverLookup)
    {
        if (!driverNumber.HasValue)
        {
            return "Unknown Team";
        }

        if (!driverLookup.TryGetValue(driverNumber.Value, out var driver))
        {
            return "Unknown Team";
        }

        return driver.TeamName ?? "Unknown Team";
    }

    private static IReadOnlyList<DriverStanding> BuildFallback()
    {
        return
        [
            new(1, "Max Verstappen", "Red Bull Racing", 400),
            new(2, "Lando Norris", "McLaren", 320),
            new(3, "Charles Leclerc", "Ferrari", 290),
            new(4, "Carlos Sainz", "Ferrari", 255),
            new(5, "Lewis Hamilton", "Mercedes", 225),
            new(6, "George Russell", "Mercedes", 210),
            new(7, "Oscar Piastri", "McLaren", 198),
            new(8, "Sergio Perez", "Red Bull Racing", 180),
            new(9, "Fernando Alonso", "Aston Martin", 162),
            new(10, "Lance Stroll", "Aston Martin", 121),
            new(11, "Yuki Tsunoda", "RB", 103),
            new(12, "Nico Hulkenberg", "Haas", 84),
            new(13, "Alexander Albon", "Williams", 73),
            new(14, "Pierre Gasly", "Alpine", 62),
            new(15, "Esteban Ocon", "Alpine", 52),
            new(16, "Kevin Magnussen", "Haas", 40),
            new(17, "Daniel Ricciardo", "RB", 33),
            new(18, "Valtteri Bottas", "Kick Sauber", 22),
            new(19, "Guanyu Zhou", "Kick Sauber", 17),
            new(20, "Logan Sargeant", "Williams", 6)
        ];
    }
}
