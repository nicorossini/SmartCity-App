using Orleans;

namespace Traffic_Service.Interfaces
{
    public interface ITrafficManagerGrain : IGrainWithIntegerKey
    {
        Task RegisterSensorAsync(string sensorId, string location);
        Task ProcessTrafficUpdateAsync(string sensorId, int vehicleCount);
        Task<List<CongestionArea>> GetMostCongestedAreasAsync(int topCount = 5);
        Task<Dictionary<string, int>> GetAllSensorDataAsync();
        Task<bool> IsAreaCongestedAsync(string sensorId);
    }
}
