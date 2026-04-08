using F1Tui;
using F1Tui.Configuration;
using F1Tui.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace F1.Infrastructure.Tests;

public sealed class StartupConfigurationTests
{
    [Fact]
    public void OptionsBinding_WithValidSettings_BindsSuccessfully()
    {
        var values = new Dictionary<string, string?>
        {
            [$"{AppOptions.SectionName}:ApiBaseUrl"] = "https://api.openf1.org/v1",
            [$"{AppOptions.SectionName}:RequestTimeoutSeconds"] = "12",
            [$"{AppOptions.SectionName}:CacheTtlMinutes"] = "45"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddPitTermApplication(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<AppOptions>>().Value;

        Assert.Equal("https://api.openf1.org/v1", options.ApiBaseUrl);
        Assert.Equal(12, options.RequestTimeoutSeconds);
        Assert.Equal(45, options.CacheTtlMinutes);
    }

    [Fact]
    public void OptionsBinding_WithInvalidApiBaseUrl_ThrowsValidationException()
    {
        var values = new Dictionary<string, string?>
        {
            [$"{AppOptions.SectionName}:ApiBaseUrl"] = "not-a-url",
            [$"{AppOptions.SectionName}:RequestTimeoutSeconds"] = "10",
            [$"{AppOptions.SectionName}:CacheTtlMinutes"] = "30"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddPitTermApplication(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => _ = provider.GetRequiredService<IOptions<AppOptions>>().Value);
    }
}
