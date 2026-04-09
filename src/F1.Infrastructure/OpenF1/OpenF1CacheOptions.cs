namespace F1.Infrastructure.OpenF1;

public sealed class OpenF1CacheOptions
{
    public const string SectionName = "OpenF1Cache";

    public int CacheTtlMinutes { get; init; } = 30;
}