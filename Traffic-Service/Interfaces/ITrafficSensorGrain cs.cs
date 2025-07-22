using Orleans;
namespace Traffic_Service.Interfaces
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
