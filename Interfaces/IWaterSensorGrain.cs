using Orleans;
using SmartCity.Interfaces.Models;

namespace SmartCity.Interfaces;

public interface IWaterSensorGrain : IGrainWithStringKey
{
    Task RegisterSensorAsync(string location, SensorType type, string zoneId); //
    Task<WaterSensorData> GetCurrentDataAsync(); //
    Task UpdateSensorDataAsync(WaterSensorData data); //
    Task SimulateDataAsync(); //
    Task<bool> IsActiveAsync(); //
    Task DeactivateSensorAsync(); //
    Task<List<WaterAlert>> CheckAnomaliesAsync(); //
}