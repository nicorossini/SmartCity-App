using System.Threading.Tasks;
using Newtonsoft.Json;
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

        public async Task SetAsync<T>(string key, T value)
        {
            var json = JsonConvert.SerializeObject(value);
            await _db.StringSetAsync(key, json);
        }
        public async Task<T?> GetAsync<T>(string key)
        {
            var json = await _db.StringGetAsync(key);
            return json.HasValue ? JsonConvert.DeserializeObject<T>(json!) : default;
        }
    }
}