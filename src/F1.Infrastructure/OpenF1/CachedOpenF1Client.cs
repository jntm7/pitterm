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
        public object Data { get; init; } = new object();
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
        var hit = TryGetCached<IReadOnlyList<OpenF1SessionDto>>(cacheKey);
        if (hit is not null)
        {
            return hit;
        }

        var result = await inner.GetSessionsAsync(year, sessionName, cancellationToken);
        SetCached(cacheKey, result.ToList());
        return result;
    }

    public async Task<IReadOnlyList<OpenF1DriverStandingDto>> GetDriverStandingsAsync(
        int year,
        int? meetingKey = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"driver-standings|year={year}|meeting_key={meetingKey?.ToString() ?? ""}";
        var hit = TryGetCached<IReadOnlyList<OpenF1DriverStandingDto>>(cacheKey);
        if (hit is not null)
        {
            return hit;
        }

        var result = await inner.GetDriverStandingsAsync(year, meetingKey, cancellationToken);
        SetCached(cacheKey, result.ToList());
        return result;
    }

    public async Task<IReadOnlyList<OpenF1ConstructorStandingDto>> GetConstructorStandingsAsync(
        int year,
        int? meetingKey = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"constructor-standings|year={year}|meeting_key={meetingKey?.ToString() ?? ""}";
        var hit = TryGetCached<IReadOnlyList<OpenF1ConstructorStandingDto>>(cacheKey);
        if (hit is not null)
        {
            return hit;
        }

        var result = await inner.GetConstructorStandingsAsync(year, meetingKey, cancellationToken);
        SetCached(cacheKey, result.ToList());
        return result;
    }

    public async Task<IReadOnlyList<OpenF1PositionDto>> GetPositionsAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"positions|meeting_key={meetingKey?.ToString() ?? ""}|session_key={sessionKey?.ToString() ?? ""}";
        var hit = TryGetCached<IReadOnlyList<OpenF1PositionDto>>(cacheKey);
        if (hit is not null)
        {
            return hit;
        }

        var result = await inner.GetPositionsAsync(meetingKey, sessionKey, cancellationToken);
        SetCached(cacheKey, result.ToList());
        return result;
    }

    public async Task<IReadOnlyList<OpenF1DriverDto>> GetDriversAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"drivers|meeting_key={meetingKey?.ToString() ?? ""}|session_key={sessionKey?.ToString() ?? ""}";
        var hit = TryGetCached<IReadOnlyList<OpenF1DriverDto>>(cacheKey);
        if (hit is not null)
        {
            return hit;
        }

        var result = await inner.GetDriversAsync(meetingKey, sessionKey, cancellationToken);
        SetCached(cacheKey, result.ToList());
        return result;
    }

    public async Task<IReadOnlyList<OpenF1WeatherDto>> GetWeatherAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"weather|meeting_key={meetingKey?.ToString() ?? ""}|session_key={sessionKey?.ToString() ?? ""}";
        var hit = TryGetCached<IReadOnlyList<OpenF1WeatherDto>>(cacheKey);
        if (hit is not null)
        {
            return hit;
        }

        var result = await inner.GetWeatherAsync(meetingKey, sessionKey, cancellationToken);
        SetCached(cacheKey, result.ToList());
        return result;
    }

    public async Task<IReadOnlyList<OpenF1PitDto>> GetPitStopsAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"pit|meeting_key={meetingKey?.ToString() ?? ""}|session_key={sessionKey?.ToString() ?? ""}";
        var hit = TryGetCached<IReadOnlyList<OpenF1PitDto>>(cacheKey);
        if (hit is not null)
        {
            return hit;
        }

        var result = await inner.GetPitStopsAsync(meetingKey, sessionKey, cancellationToken);
        SetCached(cacheKey, result.ToList());
        return result;
    }

    public async Task<IReadOnlyList<OpenF1LapDto>> GetLapsAsync(
        int? meetingKey = null,
        int? sessionKey = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"laps|meeting_key={meetingKey?.ToString() ?? ""}|session_key={sessionKey?.ToString() ?? ""}";
        var hit = TryGetCached<IReadOnlyList<OpenF1LapDto>>(cacheKey);
        if (hit is not null)
        {
            return hit;
        }

        var result = await inner.GetLapsAsync(meetingKey, sessionKey, cancellationToken);
        SetCached(cacheKey, result.ToList());
        return result;
    }

    private T? TryGetCached<T>(string cacheKey) where T : class
    {
        lock (cache)
        {
            if (cache.TryGetValue(cacheKey, out var entry) && entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
                return entry.Data as T;
            }
        }

        logger.LogDebug("Cache miss for {CacheKey}", cacheKey);
        return null;
    }

    private void SetCached(string cacheKey, object data)
    {
        lock (cache)
        {
            cache[cacheKey] = new CacheEntry
            {
                Data = data,
                ExpiresAt = DateTimeOffset.UtcNow.Add(cacheTtl)
            };
        }
    }
}
