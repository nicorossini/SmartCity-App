using Orleans;

namespace SmartCityParking.Grains.Interfaces
{
    public interface IParkingSpotGrain : IGrainWithIntegerKey
    {
        Task<bool> IsOccupiedAsync();
        Task<bool> ReserveAsync(string userId);
        Task<bool> ReleaseAsync(string userId);
        Task<string?> GetCurrentUserAsync();
        Task InitializeAsync();
    }
}