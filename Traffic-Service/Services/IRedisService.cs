namespace Traffic_Service.Services
{
    public interface IRedisService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task<T?> GetAsync<T>(string key);
        Task DeleteAsync(string key);
    }
}
