using F1.Infrastructure.OpenF1.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace F1.Infrastructure.OpenF1;

public sealed class CachedOpenF1Client : IOpenF1Client
{
    private readonly IOpenF1Client inner;
    private readonly TimeSpan cacheTtl;
    private readonly ILogger<CachedOpenF1Client> logger;
    private readonly Dictionary<string, CacheEntry> cache = new();

    private sealed class CacheEntry
    {
        public IReadOnlyList<OpenF1SessionDto> Data { get; init; } = [];
        public DateTimeOffset ExpiresAt { get; init; }
    }

    public CachedOpenF1Client(
        IOpenF1Client inner,
        IOptions<OpenF1CacheOptions> options,
        ILogger<CachedOpenF1Client> logger)
    {
        this.inner = inner;
        this.cacheTtl = TimeSpan.FromMinutes(options.Value.CacheTtlMinutes);
        this.logger = logger;
    }

    public async Task<IReadOnlyList<OpenF1SessionDto>> GetSessionsAsync(
        int year,
        string? sessionName = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"sessions|year={year}|session_name={sessionName ?? ""}";
        lock (cache)
        {
            if (cache.TryGetValue(cacheKey, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return entry.Data;
            }
        }

        logger.LogDebug("Cache miss for {CacheKey}, fetching from API", cacheKey);
        var result = await inner.GetSessionsAsync(year, sessionName, cancellationToken);

        lock (cache)
        {
            cache[cacheKey] = new CacheEntry
            {
                Data = result.ToList(),
                ExpiresAt = DateTimeOffset.UtcNow.Add(cacheTtl)
            };
        }

        return result;
    }
}