using System.Threading.Tasks;

namespace SmartCity.Services
{
    public interface IRedisCacheService
    {
        Task SetAsync(string key, string value);
        Task<string?> GetAsync(string key);
    }
}