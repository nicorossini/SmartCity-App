using SmartCity.Interfaces.Models;

namespace SmartCity.Services
{
    public interface IMongoDBService
    {
        Task SaveSensorDataAsync(WaterSensorData record);
        Task SaveZoneStatusAsync(WaterZoneStatus record);
        Task SaveAlertAsync(WaterAlert record);
        Task<bool> SensorExistsAsync(string sensorID, string zoneID);
        Task<WaterZoneStatus?> GetZoneByIdAsync(string zoneId); 
       
    }
}