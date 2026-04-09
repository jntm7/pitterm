using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.OpenF1;
using F1.Infrastructure.OpenF1.Models;

namespace F1.Infrastructure.Services;

public sealed class PitStopService : IPitStopService
{
    private readonly IOpenF1Client? openF1Client;

    public PitStopService()
    {
    }

    public PitStopService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
    }

    public async Task<IReadOnlyList<PitStop>> GetPitStopsAsync(
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
            var items = await openF1Client.GetPitStopsAsync(meetingKey, sessionKey, cancellationToken);
            var drivers = await openF1Client.GetDriversAsync(meetingKey, sessionKey, cancellationToken);
            if (drivers.Count == 0)
            {
                drivers = await openF1Client.GetDriversAsync(meetingKey, null, cancellationToken);
            }

            var driverLookup = drivers
                .Where(driver => driver.DriverNumber.HasValue)
                .GroupBy(driver => driver.DriverNumber!.Value)
                .ToDictionary(group => group.Key, group => group.First());

            return items
                .Select(item => new PitStop(
                    item.DriverNumber,
                    ResolveDriverName(item.DriverNumber, driverLookup),
                    ResolveTeamName(item.DriverNumber, driverLookup),
                    item.LapNumber,
                    item.PitDuration,
                    ParseDate(item.Date)))
                .OrderBy(item => item.Timestamp ?? DateTimeOffset.MinValue)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        return DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : null;
    }

    private static string? ResolveDriverName(int? driverNumber, IReadOnlyDictionary<int, OpenF1DriverDto> driverLookup)
    {
        if (!driverNumber.HasValue)
        {
            return null;
        }

        if (!driverLookup.TryGetValue(driverNumber.Value, out var driver))
        {
            return null;
        }

        return driver.FullName ?? driver.LastName;
    }

    private static string? ResolveTeamName(int? driverNumber, IReadOnlyDictionary<int, OpenF1DriverDto> driverLookup)
    {
        if (!driverNumber.HasValue)
        {
            return null;
        }

        if (!driverLookup.TryGetValue(driverNumber.Value, out var driver))
        {
            return null;
        }

        return driver.TeamName;
    }
}
