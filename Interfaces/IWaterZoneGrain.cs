using Orleans;
using SmartCity.Interfaces.Models;

namespace SmartCity.Interfaces;
public interface IWaterZoneGrain : IGrainWithStringKey
{
    Task RegisterZoneAsync(string name, List<string> sensorIds);
    Task<WaterZoneStatus> GetZoneStatusAsync();
    Task AddSensorAsync(string sensorId);
    Task RemoveSensorAsync(string sensorId);
    Task<List<WaterAlert>> GetActiveAlertsAsync();
    Task UpdateZoneDataAsync();
    Task<bool> IsLeakDetectedAsync();
}