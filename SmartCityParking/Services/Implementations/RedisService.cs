using Newtonsoft.Json;
using StackExchange.Redis;

namespace SmartCityParking.Services.Interfaces
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _database;

        public RedisService(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            var json = JsonConvert.SerializeObject(value);
            await _database.StringSetAsync(key, json, expiry);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var json = await _database.StringGetAsync(key);
            return json.HasValue ? JsonConvert.DeserializeObject<T>(json!) : default;
        }

        public async Task DeleteAsync(string key)
        {
            await _database.KeyDeleteAsync(key);
        }
    }
}
