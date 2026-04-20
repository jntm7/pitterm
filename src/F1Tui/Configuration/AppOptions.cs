namespace F1Tui.Configuration;

public sealed class AppOptions
{
    public const string SectionName = "PitTerm";

    public string ApiBaseUrl { get; init; } = "https://api.openf1.org/v1";
    public string DriverProfileApiBaseUrl { get; init; } = "https://api.jolpi.ca/";
    public int RequestTimeoutSeconds { get; init; } = 10;
    public int CacheTtlMinutes { get; init; } = 30;
}
