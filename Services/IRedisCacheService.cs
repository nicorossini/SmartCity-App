using System.Threading.Tasks;

namespace SmartCity.Services
{
    public interface IRedisCacheService
    {
        Task SetAsync<T>(string key, T value);
        Task<T?> GetAsync<T>(string key);
        Task DeleteAsync(string key);
    }
}