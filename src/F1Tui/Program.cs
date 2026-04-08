using F1Tui.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var serviceCollection = new ServiceCollection();
serviceCollection.AddPitTermApplication(configuration);

await using var serviceProvider = serviceCollection.BuildServiceProvider();
var app = serviceProvider.GetRequiredService<TerminalApp>();

await app.RunAsync();
