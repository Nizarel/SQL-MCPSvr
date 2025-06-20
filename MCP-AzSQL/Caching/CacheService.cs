// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MCP_Azsql.Caching;

/// <summary>
/// Caching service for database metadata to improve performance
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    
    // Convenience methods for specific cache types with predefined TTL
    Task SetTableListAsync<T>(string key, T value);
    Task SetTableSchemaAsync<T>(string key, T value);
    Task SetQueryResultAsync<T>(string key, T value);
}

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheOptions _options;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache cache, IOptions<CacheOptions> options, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        var value = _cache.Get<T>(key);
        _logger.LogDebug("Cache {Action} for key: {Key}", value != null ? "Hit" : "Miss", key);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _options.DefaultExpiration,
            SlidingExpiration = _options.SlidingExpiration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, value, options);
        _logger.LogDebug("Cache Set for key: {Key}, expiration: {Expiration}", key, options.AbsoluteExpirationRelativeToNow);
        
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Cache Remove for key: {Key}", key);
        return Task.CompletedTask;
    }    public Task RemoveByPatternAsync(string pattern)
    {
        // Memory cache doesn't support pattern removal directly
        // For production, consider using Redis or implement a tracking mechanism
        _logger.LogWarning("Pattern-based cache removal not supported in MemoryCache: {Pattern}", pattern);
        return Task.CompletedTask;
    }

    public Task SetTableListAsync<T>(string key, T value)
    {
        return SetAsync(key, value, _options.TableListExpiration);
    }

    public Task SetTableSchemaAsync<T>(string key, T value)
    {
        return SetAsync(key, value, _options.TableSchemaExpiration);
    }

    public Task SetQueryResultAsync<T>(string key, T value)
    {
        return SetAsync(key, value, _options.QueryResultExpiration);
    }
}

public class CacheOptions
{
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromMinutes(2);
    
    // Specific TTL values for different data types
    public TimeSpan TableListExpiration { get; set; } = TimeSpan.FromMinutes(35);
    public TimeSpan TableSchemaExpiration { get; set; } = TimeSpan.FromMinutes(35);
    public TimeSpan QueryResultExpiration { get; set; } = TimeSpan.FromMinutes(20);
}

/// <summary>
/// Cache keys for different data types
/// </summary>
public static class CacheKeys
{
    public const string TableList = "tables:list";
    public static string TableSchema(string tableName) => $"tables:schema:{tableName}";
    public static string QueryResult(string queryHash) => $"queries:result:{queryHash}";
}
