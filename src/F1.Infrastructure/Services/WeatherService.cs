using F1.Core.Models;
using F1.Core.Services;
using F1.Infrastructure.OpenF1;

namespace F1.Infrastructure.Services;

public sealed class WeatherService : IWeatherService
{
    private readonly IOpenF1Client? openF1Client;

    public WeatherService()
    {
    }

    public WeatherService(IOpenF1Client openF1Client)
    {
        this.openF1Client = openF1Client;
    }

    public async Task<IReadOnlyList<WeatherSample>> GetWeatherSamplesAsync(
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
            var items = await openF1Client.GetWeatherAsync(meetingKey, sessionKey, cancellationToken);
            return items
                .Select(item => new WeatherSample(
                    ParseDate(item.Date),
                    item.AirTemperature,
                    item.TrackTemperature,
                    item.Rainfall))
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
}
