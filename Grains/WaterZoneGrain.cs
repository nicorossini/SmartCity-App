using SmartCity.Interfaces;
using SmartCity.Interfaces.Models;
using SmartCity.Services;

namespace SmartCity.Grains;

public class WaterZoneGrain : Grain, IWaterZoneGrain
{
    public Task RegisterZoneAsync(string name, List<string> sensorIds)
    {
        throw new NotImplementedException();
    }

    public Task<WaterZoneStatus> GetZoneStatusAsync()
    {
        throw new NotImplementedException();
    }

    public Task AddSensorAsync(string sensorId)
    {
        throw new NotImplementedException();
    }

    public Task RemoveSensorAsync(string sensorId)
    {
        throw new NotImplementedException();
    }

    public Task<List<WaterAlert>> GetActiveAlertsAsync()
    {
        throw new NotImplementedException();
    }

    public Task UpdateZoneDataAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsLeakDetectedAsync()
    {
        throw new NotImplementedException();
    }
}