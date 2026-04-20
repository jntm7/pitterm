using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace F1.Infrastructure.Jolpica;

public sealed class JolpicaClient(HttpClient httpClient, ILogger<JolpicaClient> logger)
{
    public async Task<IReadOnlyList<JolpicaDriverDto>> GetDriversBySeasonAsync(
        int season,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<JolpicaDriverResponseDto>(
                $"ergast/f1/{season}/drivers/",
                cancellationToken);

            return response?.MrData?.DriverTable?.Drivers ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to fetch Jolpica drivers for season {Season}", season);
            return [];
        }
    }
}
