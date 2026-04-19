using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.OpenF1;

namespace F1.Infrastructure.Services;

public sealed class DriverService : IDriverService
{
    private readonly IOpenF1Client? openF1Client;

    public DriverService()
    {
    }

    public DriverService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
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

            return dtos
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
                    dto.TeamColour ?? "FFFFFF"))
                .OrderBy(d => d.TeamName)
                .ThenBy(d => d.DriverNumber)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}