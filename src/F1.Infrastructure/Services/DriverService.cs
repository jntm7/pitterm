using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.Jolpica;
using F1.Infrastructure.OpenF1;

namespace F1.Infrastructure.Services;

public sealed class DriverService : IDriverService
{
    private readonly IOpenF1Client? openF1Client;
    private readonly JolpicaClient? jolpicaClient;

    public DriverService()
    {
    }

    public DriverService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
    }

    public DriverService(IOpenF1Client openF1Client, JolpicaClient jolpicaClient)
    {
        this.openF1Client = openF1Client;
        this.jolpicaClient = jolpicaClient;
    }

    public async Task<IReadOnlyList<Driver>> GetDriversAsync(
        int? meetingKey = null,
        CancellationToken cancellationToken = default)
    {
        if (openF1Client is null)
        {
            return [];
        }

        try
        {
            var dtos = await openF1Client.GetDriversAsync(meetingKey, null, cancellationToken);
            if (dtos.Count == 0 && meetingKey.HasValue)
            {
                dtos = await openF1Client.GetDriversAsync(null, null, cancellationToken);
            }

            var openF1Drivers = dtos
                .Where(dto => dto.DriverNumber.HasValue)
                .GroupBy(dto => dto.DriverNumber!.Value)
                .Select(group => group.First())
                .Select(dto => new Driver(
                    dto.DriverNumber!.Value,
                    dto.FullName ?? dto.LastName ?? $"Driver {dto.DriverNumber}",
                    dto.FirstName ?? "",
                    dto.LastName ?? "",
                    dto.NameAcronym ?? "",
                    dto.TeamName ?? "Unknown Team",
                    dto.TeamColour ?? "FFFFFF",
                    string.Empty,
                    null,
                    string.Empty))
                .OrderBy(d => d.TeamName)
                .ThenBy(d => d.DriverNumber)
                .ToList();

            if (jolpicaClient is null)
            {
                return openF1Drivers;
            }

            var season = DateTime.UtcNow.Year;
            var jolpicaDrivers = await jolpicaClient.GetDriversBySeasonAsync(season, cancellationToken);
            if (jolpicaDrivers.Count == 0)
            {
                return openF1Drivers;
            }

            var jolpicaByNumber = jolpicaDrivers
                .Where(d => int.TryParse(d.PermanentNumber, out _))
                .Select(d => new
                {
                    Number = int.Parse(d.PermanentNumber!),
                    Driver = d
                })
                .GroupBy(x => x.Number)
                .ToDictionary(g => g.Key, g => g.First().Driver);

            return openF1Drivers
                .Select(driver =>
                {
                    if (!jolpicaByNumber.TryGetValue(driver.DriverNumber, out var profile))
                    {
                        return driver;
                    }

                    DateOnly? dob = null;
                    if (!string.IsNullOrWhiteSpace(profile.DateOfBirth)
                        && DateOnly.TryParse(profile.DateOfBirth, out var parsedDob))
                    {
                        dob = parsedDob;
                    }

                    return driver with
                    {
                        Nationality = profile.Nationality ?? string.Empty,
                        DateOfBirth = dob,
                        ProfileUrl = profile.Url ?? string.Empty
                    };
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
