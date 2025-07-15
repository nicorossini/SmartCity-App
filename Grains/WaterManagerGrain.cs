using SmartCity.Interfaces;
using SmartCity.Interfaces.Models;
using SmartCity.Services;

namespace SmartCity.Grains;

public class WaterManagerGrain : Grain, IWaterManagerGrain
{
    public Task<List<WaterZoneStatus>> GetAllZonesStatusAsync()
    {
        throw new NotImplementedException();
    }

    public Task<List<WaterAlert>> GetCriticalAlertsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<WaterSensorData> GetSensorDataAsync(string sensorId)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, double>> GetSystemOverviewAsync()
    {
        throw new NotImplementedException();
    }

    public Task InitializeTestDataAsync()
    {
        throw new NotImplementedException();
    }
}