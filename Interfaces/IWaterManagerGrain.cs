using Orleans;
using SmartCity.Interfaces.Models;

namespace SmartCity.Interfaces;
public interface IWaterManagerGrain : IGrainWithStringKey
{
    Task<List<WaterZoneStatus>> GetAllZonesStatusAsync(); //
    Task<List<WaterAlert>> GetCriticalAlertsAsync(); //
    Task<WaterSensorData> GetSensorDataAsync(string sensorId); //
    Task<Dictionary<string, double>> GetSystemOverviewAsync(); //
    Task InitializeTestDataAsync(); //
}