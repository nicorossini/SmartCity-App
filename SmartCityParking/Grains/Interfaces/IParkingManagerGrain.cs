using Orleans;

namespace SmartCityParking.Grains.Interfaces
{
    public interface IParkingManagerGrain : IGrainWithStringKey
    {
        Task<int?> ReserveParkingSpotAsync(string userId);
        Task<bool> ReleaseParkingSpotAsync(string userId);
        Task<ParkingStatus> GetParkingStatusAsync();
        Task InitializeAsync(int totalSpots);
    }
}