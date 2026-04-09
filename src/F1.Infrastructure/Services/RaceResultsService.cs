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
            return [];
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

            if (results.Count > 0 && IsPlausibleResults(results))
            {
                return results;
            }
        }
        catch
        {
        }

        return [];
    }

    private static bool IsPlausibleResults(IReadOnlyList<RaceResult> results)
    {
        if (results.Count < 10)
        {
            return false;
        }

        var uniqueDrivers = results
            .Select(result => result.DriverName.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (uniqueDrivers < 10)
        {
            return false;
        }

        var duplicatePositions = results
            .GroupBy(result => result.Position)
            .Any(group => group.Count() > 1);

        return !duplicatePositions;
    }

    private static DateTimeOffset ParseDate(string? date)
    {
        return DateTimeOffset.TryParse(date, out var parsed)
            ? parsed
            : DateTimeOffset.MinValue;
    }

}
