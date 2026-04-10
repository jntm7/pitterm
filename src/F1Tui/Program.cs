using F1Tui;
using F1Tui.Configuration;
using F1Tui.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var environmentName = Environment.GetEnvironmentVariable(StartupKeys.EnvironmentVariableName)
    ?? StartupKeys.ProductionEnvironmentName;
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables(prefix: StartupKeys.EnvironmentVariablesPrefix)
    .Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddPitTermApplication(configuration);

await using var serviceProvider = serviceCollection.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

using var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

try
{
    logger.LogInformation("PitTerm startup environment: {EnvironmentName}", environmentName);
    var app = serviceProvider.GetRequiredService<TerminalApp>();
    await app.RunAsync(cancellationTokenSource.Token);
}
catch (OptionsValidationException ex)
{
    logger.LogError(ex, "Configuration validation failed at startup.");
    Environment.ExitCode = 1;
}
catch (OperationCanceledException)
{
    logger.LogInformation("PitTerm shutdown requested by user.");
}
