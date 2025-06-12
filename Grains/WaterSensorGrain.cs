using Orleans;
using SmartCity.Interfaces;
using SmartCity.Interfaces.Models;
using SmartCity.Services;

namespace SmartCity.Grains;

public class WaterSensorGrain : Grain, IWaterSensorGrain
{
    private readonly MongoDbService _mongoService;
    private readonly RedisCacheService _redisService;
    private readonly RabbitMQService _rabbitMQService;
    private readonly ILogger<WaterSensorGrain> _logger;
    
    private WaterSensorData? _currentData;
    private string _location = string.Empty;
    private SensorType _sensorType;
    private string _zoneId = string.Empty;
    private bool _isActive;
    private Timer? _simulationTimer;

    public WaterSensorGrain(
        MongoDbService mongoService,
        RedisCacheService redisService,
        RabbitMQService rabbitMQService,
        ILogger<WaterSensorGrain> logger)
    {
        _mongoService = mongoService;
        _redisService = redisService;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    public async Task RegisterSensorAsync(string location, SensorType type, string zoneId)
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

        // Cache current data in Redis
        await _redisService.SetAsync("water_current_data", _currentData);
        
        // Save to MongoDB
        await _mongoService.SaveSensorDataAsync(_currentData);
        
        // Start automatic simulation
        _simulationTimer = new Timer(async _ => await SimulateDataAsync(), 
            null, TimeSpan.Zero, TimeSpan.FromMinutes(2));
        
        _logger.LogInformation("Water sensor {SensorId} registered at {Location} in zone {ZoneId}", 
            this.GetPrimaryKeyString(), location, zoneId);
        
        // Notify zone about new sensor
        var zoneGrain = GrainFactory.GetGrain<IWaterZoneGrain>(zoneId);
        await zoneGrain.AddSensorAsync(this.GetPrimaryKeyString());
    }

    public Task<WaterSensorData> GetCurrentDataAsync()
    {
        return Task.FromResult(_currentData ?? throw new InvalidOperationException("Sensor not registered"));
    }

    public async Task UpdateSensorDataAsync(WaterSensorData data)
    {
        _currentData = data with { 
            SensorId = this.GetPrimaryKeyString(),
            Timestamp = DateTime.UtcNow 
        };
        
        // Update cache
        await _redisService.SetAsync("update_with_water_current_data", _currentData);
        
        // Save to MongoDB
        await _mongoService.SaveSensorDataAsync(_currentData);
        
        // Check for anomalies and publish alerts
        var alerts = await CheckAnomaliesAsync();
        foreach (var alert in alerts)
        {
            await _rabbitMQService.PublishAlertAsync(alert);
        }
        
        // Notify zone grain
        var zoneGrain = GrainFactory.GetGrain<IWaterZoneGrain>(_zoneId);
        await zoneGrain.UpdateZoneDataAsync();
    }

    public async Task SimulateDataAsync()
    {
        if (!_isActive || _currentData == null) return;
        
        var now = DateTime.UtcNow;
        var hour = now.Hour;
        
        // Simulate realistic water distribution patterns
        var simulatedData = _currentData with
        {
            FlowRate = SimulateFlowRate(hour, _sensorType),
            Pressure = SimulatePressure(hour, _currentData.Pressure),
            Temperature = SimulateTemperature(hour),
            pH = SimulatepH(_currentData.pH),
            TurbidityNTU = SimulateTurbidity(_currentData.TurbidityNTU),
            Timestamp = now
        };
        
        await UpdateSensorDataAsync(simulatedData);
    }

    public Task<bool> IsActiveAsync()
    {
        return Task.FromResult(_isActive);
    }

    public Task DeactivateSensorAsync()
    {
        _isActive = false;
        _simulationTimer?.Dispose();
        _logger.LogInformation("Water sensor {SensorId} deactivated", this.GetPrimaryKeyString());
        return Task.CompletedTask;
    }

    public async Task<List<WaterAlert>> CheckAnomaliesAsync()
    {
        var alerts = new List<WaterAlert>();
        
        if (_currentData == null) return alerts;
        
        // Check pressure anomalies
        if (_currentData.Pressure < 30) // Low pressure threshold
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
        else if (_currentData.Pressure > 100) // High pressure threshold
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
        
        // Check water quality
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
        
        if (_currentData.TurbidityNTU > 4.0) // High turbidity
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

    // Private simulation methods
    private double SimulateFlowRate(int hour, SensorType sensorType)
    {
        var baseFlow = sensorType switch
        {
            SensorType.FlowMeter => 150.0, // L/min
            SensorType.Mixed => 100.0,
            _ => 50.0
        };
        
        // Simulate daily usage patterns
        var multiplier = hour switch
        {
            >= 6 and <= 9 => 1.8,    // Morning peak
            >= 10 and <= 16 => 1.0,  // Day time
            >= 17 and <= 21 => 1.6,  // Evening peak
            >= 22 or <= 5 => 0.3,    // Night time
            _ => 1.0
        };
        
        var random = new Random();
        var variation = 0.8 + (random.NextDouble() * 0.4); // ±20% variation
        
        return baseFlow * multiplier * variation;
    }
    
    private double SimulatePressure(int hour, double currentPressure)
    {
        var targetPressure = hour switch
        {
            >= 6 and <= 9 => 45.0,   // Lower during morning peak
            >= 10 and <= 16 => 60.0, // Normal pressure
            >= 17 and <= 21 => 50.0, // Slightly lower during evening peak
            _ => 65.0                 // Higher at night
        };
        
        // Gradual pressure changes
        var pressureChange = (targetPressure - currentPressure) * 0.1;
        var random = new Random();
        var noise = (random.NextDouble() - 0.5) * 5; // ±2.5 PSI noise
        
        return Math.Max(10, currentPressure + pressureChange + noise);
    }
    
    private double SimulateTemperature(int hour)
    {
        var baseTemp = 15.0; // °C
        var dailyVariation = Math.Sin((hour - 6) * Math.PI / 12) * 3; // ±3°C daily variation
        var random = new Random();
        var noise = (random.NextDouble() - 0.5) * 2; // ±1°C noise
        
        return baseTemp + dailyVariation + noise;
    }
    
    private double SimulatepH(double currentpH)
    {
        var targetpH = 7.2; // Slightly alkaline
        var change = (targetpH - currentpH) * 0.05; // Slow pH changes
        var random = new Random();
        var noise = (random.NextDouble() - 0.5) * 0.2; // ±0.1 pH noise
        
        return Math.Max(6.0, Math.Min(8.5, currentpH + change + noise));
    }
    
    private double SimulateTurbidity(double currentTurbidity)
    {
        var targetTurbidity = 1.0; // Low turbidity target
        var change = (targetTurbidity - currentTurbidity) * 0.1;
        var random = new Random();
        var noise = random.NextDouble() * 0.5; // Positive noise only
        
        return Math.Max(0.1, currentTurbidity + change + noise);
    }
    
    private double GetInitialFlowRate() => 100.0 + (new Random().NextDouble() * 50);
    private double GetInitialPressure() => 50.0 + (new Random().NextDouble() * 20);
    private double GetInitialTemperature() => 15.0 + (new Random().NextDouble() * 5);
    private double GetInitialpH() => 7.0 + (new Random().NextDouble() * 0.5);
    private double GetInitialTurbidity() => 0.5 + (new Random().NextDouble() * 1.0);

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _simulationTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}