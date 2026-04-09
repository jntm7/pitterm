using F1.Core.Services;
using F1.Infrastructure.Services;
using F1Tui.Configuration;
using F1Tui.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace F1Tui.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPitTermApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AppOptions>()
            .Bind(configuration.GetSection(AppOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<AppOptions>, AppOptionsValidator>();

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("F1Tui", LogLevel.Information);
            logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            logging.AddFilter("Microsoft.Extensions.Http", LogLevel.Warning);
        });

        services.AddSingleton<ISeasonService, SeasonService>();
        services.AddHttpClient<IRaceService, RaceService>((serviceProvider, client) =>
        {
            var appOptions = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;
            client.BaseAddress = new Uri($"{appOptions.ApiBaseUrl.TrimEnd('/')}/");
            client.Timeout = TimeSpan.FromSeconds(appOptions.RequestTimeoutSeconds);
        });

        services.AddHttpClient<ISessionService, SessionService>((serviceProvider, client) =>
        {
            var appOptions = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;
            client.BaseAddress = new Uri($"{appOptions.ApiBaseUrl.TrimEnd('/')}/");
            client.Timeout = TimeSpan.FromSeconds(appOptions.RequestTimeoutSeconds);
        });
        services.AddSingleton<IAppStateStore, InMemoryAppStateStore>();
        services.AddSingleton<TerminalApp>();

        return services;
    }
}
