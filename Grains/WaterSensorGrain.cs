using Orleans;
using SmartCity.Interfaces;
using SmartCity.Interfaces.Models;
using SmartCity.Services;

namespace SmartCity.Grains;

public class WaterSensorGrain : Grain, IWaterSensorGrain
{
    private readonly IMongoDBService _mongoService;
    private readonly IRedisCacheService _redisService;
    private readonly ILogger<WaterSensorGrain> _logger;

    private WaterSensorData? _currentData;
    private string _location = string.Empty;
    private SensorType _sensorType;
    private string _zoneId = string.Empty;
    private bool _isActive;


    public WaterSensorGrain(
        IMongoDBService mongoService,
        IRedisCacheService redisService,
        ILogger<WaterSensorGrain> logger)
    {
        _mongoService = mongoService;
        _redisService = redisService;
        _logger = logger;
    }
   
    public async Task<bool> RegisterSensorAsync(string location, SensorType type, string zoneId)
    {
        _location = location;
        _sensorType = type;
        _zoneId = zoneId;
        _isActive = true;

        _currentData = new WaterSensorData
        {
            SensorId = this.GetPrimaryKeyString(),
            Location = location,
            Type = type,
            ZoneId = zoneId,
            FlowRate = GetInitialFlowRate(),
            Pressure = GetInitialPressure(),
            Temperature = GetInitialTemperature(),
            pH = GetInitialpH(),
            TurbidityNTU = GetInitialTurbidity(),
            Timestamp = DateTime.UtcNow,
            IsAnomalous = false
        };

        if (await _mongoService.SensorExistsAsync(_currentData.SensorId, _currentData.ZoneId))
        {
            _logger.LogInformation("Water sensor {SensorId} already exists", this.GetPrimaryKeyString());
            return false;
        }
        else
        {
            await _redisService.SetAsync("water_current_data", _currentData);

            await _mongoService.SaveSensorDataAsync(_currentData);

            _logger.LogInformation("Water sensor {SensorId} registered at {Location} in zone {ZoneId}",
                this.GetPrimaryKeyString(), location, zoneId);

            var zoneGrain = GrainFactory.GetGrain<IWaterZoneGrain>(zoneId);
            await zoneGrain.AddSensorAsync(this.GetPrimaryKeyString());


            return true;
        }
    }

    public Task<WaterSensorData> GetCurrentDataAsync()
    {
        return Task.FromResult(_currentData ?? throw new InvalidOperationException("Sensor not registered"));
    }

    public Task<bool> IsActiveAsync()
    {
        return Task.FromResult(_isActive);
    }

    public Task ActiveSensorAsync()
    {
        _isActive = true;
        _logger.LogInformation("Water sensor {SensorId} activated", this.GetPrimaryKeyString());
        if (!string.IsNullOrEmpty(_zoneId))
        {
            var zoneGrain = GrainFactory.GetGrain<IWaterZoneGrain>(_zoneId);
            _ = zoneGrain.UpdateZoneDataAsync();
        }

        return Task.CompletedTask;
    }

    public Task DeactivateSensorAsync()
    {
        _isActive = false;
        _logger.LogInformation("Water sensor {SensorId} deactivated", this.GetPrimaryKeyString());
        if (!string.IsNullOrEmpty(_zoneId))
        {
            var zoneGrain = GrainFactory.GetGrain<IWaterZoneGrain>(_zoneId);
            _ = zoneGrain.UpdateZoneDataAsync();
        }
        
        return Task.CompletedTask;
    }

    public async Task<List<WaterAlert>> CheckAnomaliesAsync()
    {
        var alerts = new List<WaterAlert>();

        if (_currentData == null) return alerts;

        if (_currentData.Pressure < 30) 
        {
            alerts.Add(new WaterAlert
            {
                Type = AlertType.LowPressure,
                SensorId = _currentData.SensorId,
                ZoneId = _currentData.ZoneId,
                Message = $"Low pressure detected: {_currentData.Pressure:F1} PSI",
                Severity = _currentData.Pressure < 20 ? AlertSeverity.Critical : AlertSeverity.Warning
            });
        }
        else if (_currentData.Pressure > 100) 
        {
            alerts.Add(new WaterAlert
            {
                Type = AlertType.HighPressure,
                SensorId = _currentData.SensorId,
                ZoneId = _currentData.ZoneId,
                Message = $"High pressure detected: {_currentData.Pressure:F1} PSI",
                Severity = _currentData.Pressure > 120 ? AlertSeverity.Critical : AlertSeverity.Warning
            });
        }

        if (_currentData.pH < 6.5 || _currentData.pH > 8.5)
        {
            alerts.Add(new WaterAlert
            {
                Type = AlertType.PoorWaterQuality,
                SensorId = _currentData.SensorId,
                ZoneId = _currentData.ZoneId,
                Message = $"pH out of range: {_currentData.pH:F2}",
                Severity = AlertSeverity.Warning
            });
        }

        if (_currentData.TurbidityNTU > 4.0)
        {
            alerts.Add(new WaterAlert
            {
                Type = AlertType.PoorWaterQuality,
                SensorId = _currentData.SensorId,
                ZoneId = _currentData.ZoneId,
                Message = $"High turbidity: {_currentData.TurbidityNTU:F2} NTU",
                Severity = _currentData.TurbidityNTU > 10 ? AlertSeverity.Critical : AlertSeverity.Warning
            });
        }

        // Save alerts to database
        foreach (var alert in alerts)
        {
            await _mongoService.SaveAlertAsync(alert);
        }

        return alerts;
    }
    
    private double GetInitialFlowRate() => 40.0 + (new Random().NextDouble() * (130.0 - 40.0));
    private double GetInitialPressure() => 5.0 + (new Random().NextDouble() * (120.0 - 5.0));
    private double GetInitialTemperature() => 10.0 + (new Random().NextDouble() * (30.0 - 10.0));
    private double GetInitialpH() => 4.5 + (new Random().NextDouble() * (9.0 - 4.5));
    private double GetInitialTurbidity() => 1.0 + (new Random().NextDouble() * (5.0 - 1.0));

}