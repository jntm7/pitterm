using Microsoft.Extensions.Options;

namespace F1Tui.Configuration;

public sealed class AppOptionsValidator : IValidateOptions<AppOptions>
{
    public ValidateOptionsResult Validate(string? name, AppOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiBaseUrl))
        {
            return ValidateOptionsResult.Fail("PitTerm:ApiBaseUrl must be provided.");
        }

        if (!Uri.TryCreate(options.ApiBaseUrl, UriKind.Absolute, out _))
        {
            return ValidateOptionsResult.Fail("PitTerm:ApiBaseUrl must be a valid absolute URI.");
        }

        if (options.RequestTimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail("PitTerm:RequestTimeoutSeconds must be greater than zero.");
        }

        if (options.CacheTtlMinutes <= 0)
        {
            return ValidateOptionsResult.Fail("PitTerm:CacheTtlMinutes must be greater than zero.");
        }

        return ValidateOptionsResult.Success;
    }
}
