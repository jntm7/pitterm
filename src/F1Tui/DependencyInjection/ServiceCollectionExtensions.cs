using F1.Core.Services;
using F1.Infrastructure.Services;
using F1Tui.Configuration;
using F1Tui.State;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            logging.SetMinimumLevel(LogLevel.Information);
        });

        services.AddSingleton<ISeasonService, SeasonService>();
        services.AddSingleton<IRaceService, RaceService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IAppStateStore, InMemoryAppStateStore>();
        services.AddSingleton<TerminalApp>();

        return services;
    }
}
