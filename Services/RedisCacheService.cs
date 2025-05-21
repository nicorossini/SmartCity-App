using System.Threading.Tasks;
using StackExchange.Redis;
using SmartCity.Services;

namespace SmartCity.Services
{
    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDatabase _db;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public Task SetAsync(string key, string value) => _db.StringSetAsync(key, value);
        public async Task<string?> GetAsync(string key)
        {
            var result = await _db.StringGetAsync(key);
            return result.HasValue ? result.ToString() : null;
        }
    }
}