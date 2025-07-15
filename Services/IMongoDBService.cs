using SmartCity.Interfaces.Models;

namespace SmartCity.Services
{
    public interface IMongoDBService
    {
        Task SaveSensorDataAsync(WaterSensorData record);
        Task SaveZoneStatusAsync(WaterZoneStatus record);
        Task SaveAlertAsync(WaterAlert record);
        Task<List<WaterSensorData>> GetSensorHistoryAsync(string sensorId, DateTime from, DateTime to);
    }
}