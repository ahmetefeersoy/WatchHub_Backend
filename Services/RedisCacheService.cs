using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Text;

namespace api.Services
{
    public interface IRedisCacheService
    {
        Task<T?> GetCacheValueAsync<T>(string key);
        Task SetCacheValueAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveCacheValueAsync(string key);
        Task RemoveByPatternAsync(string pattern);
    }

    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetCacheValueAsync<T>(string key)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedData))
                {
                    return default;
                }

                return JsonConvert.DeserializeObject<T>(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache value for key: {Key}", key);
                return default;
            }
        }

        public async Task SetCacheValueAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var serializedData = JsonConvert.SerializeObject(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
                };

                await _cache.SetStringAsync(key, serializedData, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache value for key: {Key}", key);
            }
        }

        public async Task RemoveCacheValueAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache value for key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                // Pattern-based deletion için StackExchange.Redis kullanılabilir
                // Şimdilik tek key silme yapıyoruz
                await _cache.RemoveAsync(pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache by pattern: {Pattern}", pattern);
            }
        }
    }
}
