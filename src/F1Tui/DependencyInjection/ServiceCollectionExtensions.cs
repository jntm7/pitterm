using F1.Core.Services;
using F1.Infrastructure.OpenF1;
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

        services
            .AddOptions<OpenF1CacheOptions>()
            .Bind(configuration.GetSection(OpenF1CacheOptions.SectionName));

        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddFilter("F1Tui", LogLevel.Information);
            logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
            logging.AddFilter("Microsoft.Extensions.Http", LogLevel.Warning);
        });

        services.AddSingleton<ISeasonService>(serviceProvider =>
        {
            var openF1Client = serviceProvider.GetRequiredService<IOpenF1Client>();
            return new SeasonService(openF1Client);
        });
        services.AddHttpClient<OpenF1Client>((serviceProvider, client) =>
        {
            var appOptions = serviceProvider.GetRequiredService<IOptions<AppOptions>>().Value;
            client.BaseAddress = new Uri($"{appOptions.ApiBaseUrl.TrimEnd('/')}/");
            client.Timeout = TimeSpan.FromSeconds(appOptions.RequestTimeoutSeconds);
        });

        services.AddSingleton<IOpenF1Client>(serviceProvider =>
        {
            var inner = serviceProvider.GetRequiredService<OpenF1Client>();
            var options = serviceProvider.GetRequiredService<IOptions<OpenF1CacheOptions>>();
            var logger = serviceProvider.GetRequiredService<ILogger<CachedOpenF1Client>>();
            return new CachedOpenF1Client(inner, options, logger);
        });

        services.AddSingleton<IRaceService>(serviceProvider =>
        {
            var openF1Client = serviceProvider.GetRequiredService<IOpenF1Client>();
            return new RaceService(openF1Client);
        });
        services.AddSingleton<ISessionService>(serviceProvider =>
        {
            var openF1Client = serviceProvider.GetRequiredService<IOpenF1Client>();
            return new SessionService(openF1Client);
        });
        services.AddSingleton<IRaceResultsService>(serviceProvider =>
        {
            var openF1Client = serviceProvider.GetRequiredService<IOpenF1Client>();
            return new RaceResultsService(openF1Client);
        });
        services.AddSingleton<IDriverStandingsService>(serviceProvider =>
        {
            var openF1Client = serviceProvider.GetRequiredService<IOpenF1Client>();
            return new DriverStandingsService(openF1Client);
        });
        services.AddSingleton<IConstructorStandingsService>(serviceProvider =>
        {
            var openF1Client = serviceProvider.GetRequiredService<IOpenF1Client>();
            return new ConstructorStandingsService(openF1Client);
        });
        services.AddSingleton<IAppStateStore, InMemoryAppStateStore>();
        services.AddSingleton<TerminalApp>();

        return services;
    }
}
