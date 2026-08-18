using System.Text.Json;
using FraudWatch.Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FraudWatch.Infrastructure.Caching;

/// <summary>
/// Redis-backed distributed cache implementation.
/// Uses IDistributedCache for standard operations and IConnectionMultiplexer
/// for advanced features like prefix-based invalidation.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private const string KeyPrefix = "FraudWatch:";

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = $"{KeyPrefix}{key}";
            var data = await _distributedCache.GetStringAsync(fullKey, cancellationToken);

            if (data is null)
            {
                _logger.LogDebug("Cache miss for key {CacheKey}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key {CacheKey}", key);
            return JsonSerializer.Deserialize<T>(data, JsonOptions);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection failed for GET {CacheKey}. Returning null.", key);
            return default;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize cache entry for key {CacheKey}", key);
            return default;
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = $"{KeyPrefix}{key}";
            var serialized = JsonSerializer.Serialize(value, JsonOptions);

            var options = new DistributedCacheEntryOptions();

            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default expiration of 1 hour
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            // Also set a sliding expiration to keep frequently accessed items alive
            options.SlidingExpiration = TimeSpan.FromMinutes(15);

            await _distributedCache.SetStringAsync(fullKey, serialized, options, cancellationToken);

            _logger.LogDebug("Cached key {CacheKey} with expiration {Expiration}",
                key, expiration ?? TimeSpan.FromHours(1));
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection failed for SET {CacheKey}. Continuing without cache.", key);
        }
    }

    /// <inheritdoc/>
    public async Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullKey = $"{KeyPrefix}{key}";
            await _distributedCache.RemoveAsync(fullKey, cancellationToken);
            _logger.LogDebug("Invalidated cache key {CacheKey}", key);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex, "Redis connection failed for DELETE {CacheKey}", key);
        }
    }

    /// <inheritdoc/>
    public async Task InvalidateByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPrefix = $"{KeyPrefix}{prefix}";
            var server = _connectionMultiplexer.GetServer(
                _connectionMultiplexer.GetEndPoints().First());

            var keys = server.Keys(pattern: $"{fullPrefix}*").ToArray();

            if (keys.Length > 0)
            {
                var db = _connectionMultiplexer.GetDatabase();
                await db.KeyDeleteAsync(keys);

                _logger.LogInformation(
                    "Invalidated {Count} cache keys with prefix {Prefix}",
                    keys.Length, prefix);
            }
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning(ex,
                "Redis connection failed for prefix invalidation {Prefix}", prefix);
        }
    }
}
