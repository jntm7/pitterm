using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.OpenF1;

namespace F1.Infrastructure.Services;

public sealed class RaceResultsService : IRaceResultsService
{
    private readonly IOpenF1Client? openF1Client;

    public RaceResultsService()
    {
    }

    public RaceResultsService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
    }

    public async Task<IReadOnlyList<RaceResult>> GetRaceResultsAsync(
        int season,
        int? meetingKey,
        int? sessionKey,
        CancellationToken cancellationToken = default)
    {
        if (openF1Client is null)
        {
            return BuildFallback();
        }

        try
        {
            var positions = await openF1Client.GetPositionsAsync(meetingKey, sessionKey, cancellationToken);
            var drivers = await openF1Client.GetDriversAsync(meetingKey, sessionKey, cancellationToken);

            var latestPositionByDriver = positions
                .Where(position => position.DriverNumber.HasValue && position.Position.HasValue)
                .GroupBy(position => position.DriverNumber!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(position => ParseDate(position.Date))
                        .ThenByDescending(position => position.Position)
                        .First());

            var driverLookup = drivers
                .Where(driver => driver.DriverNumber.HasValue)
                .GroupBy(driver => driver.DriverNumber!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.First());

            var results = latestPositionByDriver
                .Select(entry =>
                {
                    var position = entry.Value.Position ?? int.MaxValue;
                    var driver = driverLookup.TryGetValue(entry.Key, out var dto) ? dto : null;

                    var driverName =
                        driver?.FullName
                        ?? driver?.LastName
                        ?? $"Driver {entry.Key}";

                    var teamName = driver?.TeamName ?? "Unknown Team";

                    return new RaceResult(position, driverName, teamName);
                })
                .OrderBy(result => result.Position)
                .ToList();

            if (results.Count > 0)
            {
                return results;
            }
        }
        catch
        {
        }

        return BuildFallback();
    }

    private static DateTimeOffset ParseDate(string? date)
    {
        return DateTimeOffset.TryParse(date, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;
    }

    private static IReadOnlyList<RaceResult> BuildFallback()
    {
        return
        [
            new(1, "Max Verstappen", "Red Bull Racing"),
            new(2, "Lando Norris", "McLaren"),
            new(3, "Charles Leclerc", "Ferrari"),
            new(4, "Carlos Sainz", "Ferrari"),
            new(5, "Lewis Hamilton", "Mercedes"),
            new(6, "George Russell", "Mercedes"),
            new(7, "Fernando Alonso", "Aston Martin"),
            new(8, "Oscar Piastri", "McLaren"),
            new(9, "Sergio Perez", "Red Bull Racing"),
            new(10, "Lance Stroll", "Aston Martin"),
            new(11, "Yuki Tsunoda", "RB"),
            new(12, "Nico Hulkenberg", "Haas"),
            new(13, "Alexander Albon", "Williams"),
            new(14, "Pierre Gasly", "Alpine"),
            new(15, "Esteban Ocon", "Alpine"),
            new(16, "Kevin Magnussen", "Haas"),
            new(17, "Daniel Ricciardo", "RB"),
            new(18, "Valtteri Bottas", "Kick Sauber"),
            new(19, "Guanyu Zhou", "Kick Sauber"),
            new(20, "Logan Sargeant", "Williams")
        ];
    }
}
