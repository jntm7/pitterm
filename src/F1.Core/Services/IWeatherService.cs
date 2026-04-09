using F1.Core.Models;

namespace F1.Core.Services;

public interface IWeatherService
{
    Task<IReadOnlyList<WeatherSample>> GetWeatherSamplesAsync(
        int? meetingKey,
        int? sessionKey,
        CancellationToken cancellationToken = default);
}
