using Orleans;
using SmartCity.Interfaces.Models;

namespace SmartCity.Interfaces;

public interface IWaterSensorGrain : IGrainWithStringKey
{
    Task<bool> RegisterSensorAsync(string location, SensorType type, string zoneId); 
    Task<WaterSensorData> GetCurrentDataAsync();
    Task ActiveSensorAsync();
    Task<bool> IsActiveAsync(); 
    Task DeactivateSensorAsync(); 
    Task<List<WaterAlert>> CheckAnomaliesAsync(); 
}