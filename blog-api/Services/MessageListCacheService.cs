using System.Text.Json;
using BlogApi.Dtos;
using Microsoft.Extensions.Caching.Distributed;

namespace BlogApi.Services;

/// <summary>
/// 留言列表 Cache-Aside：读先查 Redis，未命中查 MySQL 并写入；写/删后 bump 版本号使旧缓存失效。
/// </summary>
public class MessageListCacheService(
    IDistributedCache cache,
    IConfiguration configuration,
    ILogger<MessageListCacheService> logger)
{
  private const string VersionKey = "messages:list:version";

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  private TimeSpan CacheTtl => TimeSpan.FromMinutes(
      configuration.GetValue("Redis:MessageListCacheMinutes", 10));

  public async Task<PagedMessageResponse?> TryGetAsync(
      int page, int pageSize, CancellationToken cancellationToken = default)
  {
    try
    {
      var version = await GetVersionAsync(cancellationToken);
      var key = BuildListKey(version, page, pageSize);
      var json = await cache.GetStringAsync(key, cancellationToken);
      if (json is null) return null;

      logger.LogDebug("Message list cache hit: {Key}", key);
      return JsonSerializer.Deserialize<PagedMessageResponse>(json, JsonOptions);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Redis read failed, falling back to database");
      return null;
    }
  }

  public async Task SetAsync(
      int page, int pageSize, PagedMessageResponse data, CancellationToken cancellationToken = default)
  {
    try
    {
      var version = await GetVersionAsync(cancellationToken);
      var key = BuildListKey(version, page, pageSize);
      var json = JsonSerializer.Serialize(data, JsonOptions);
      await cache.SetStringAsync(
          key,
          json,
          new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
          cancellationToken);
      logger.LogDebug("Message list cached: {Key}, TTL {Minutes}m", key, CacheTtl.TotalMinutes);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Redis write failed");
    }
  }

  /// <summary>新增或删除留言后调用，递增版本号，使所有分页列表缓存逻辑失效（旧 key 靠 TTL 自动过期）。</summary>
  public async Task InvalidateAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      var version = await GetVersionAsync(cancellationToken);
      var next = long.TryParse(version, out var n) ? n + 1 : 1;
      await cache.SetStringAsync(VersionKey, next.ToString(), cancellationToken);
      logger.LogInformation("Message list cache invalidated, version {Version}", next);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Redis cache invalidation failed");
    }
  }

  private async Task<string> GetVersionAsync(CancellationToken cancellationToken)
  {
    var version = await cache.GetStringAsync(VersionKey, cancellationToken);
    return string.IsNullOrEmpty(version) ? "0" : version;
  }

  private static string BuildListKey(string version, int page, int pageSize) =>
      $"messages:list:v{version}:p{page}:s{pageSize}";
}
