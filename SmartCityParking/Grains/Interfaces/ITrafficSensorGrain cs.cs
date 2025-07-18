using Orleans;
namespace SmartCityParking.Grains.Interfaces
{
    public interface ITrafficSensorGrain : IGrainWithStringKey
    {
        Task<int> GetCurrentVehicleCountAsync();
        Task UpdateVehicleCountAsync(int count);
        Task<TrafficSensorData> GetSensorDataAsync();
        Task StartPeriodicUpdatesAsync();
        Task StopPeriodicUpdatesAsync();
    }
}
