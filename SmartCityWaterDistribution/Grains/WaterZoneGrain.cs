using SmartCity.Interfaces;
using SmartCity.Interfaces.Models;
using SmartCity.Services;

namespace SmartCity.Grains;


public class WaterZoneGrain : Grain, IWaterZoneGrain
{
    private readonly IMongoDBService _mongoService;
    private readonly IRedisCacheService _redisService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<WaterZoneGrain> _logger;
    private string _name = string.Empty;
    private List<string> _sensorIds = new();
    private string _zoneId = "";
    private List<WaterAlert> _activeAlerts = new();
    private WaterZoneStatus? _currentStatus;
    private DateTime _lastUpdate = DateTime.UtcNow;

    public WaterZoneGrain(
        IMongoDBService mongoService,
        IRedisCacheService redisService,
        IRabbitMQService rabbitMQService,
        ILogger<WaterZoneGrain> logger)
    {
        _mongoService = mongoService;
        _redisService = redisService;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _zoneId = this.GetPrimaryKeyString();
        var zone = await _mongoService.GetZoneByIdAsync(_zoneId);
        if (zone != null)
        {
            _sensorIds = zone.ActiveSensors;
        }

        this.RegisterGrainTimer<object?>(
            callback: UpdateZoneDataTimerCallback,
            state: null,
            options: new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(30),   
                Period = TimeSpan.FromSeconds(10), 
                
            }
        );
    }

    private Task UpdateZoneDataTimerCallback(object? state, CancellationToken cancellationToken)
    {
        return UpdateZoneDataAsync();
    }
    
    public Task AddSensorAsync(string sensorId)
    {
        if (!_sensorIds.Contains(sensorId))
        {
            _sensorIds.Add(sensorId);
            _logger.LogInformation("Sensor {SensorId} added to zone {ZoneId}", sensorId, this.GetPrimaryKeyString());

            return _mongoService.SaveZoneStatusAsync(new WaterZoneStatus
            {
                ZoneId = _zoneId,
                ActiveSensors = _sensorIds
            });

        }

        return Task.CompletedTask;
    }

    public async Task RegisterZoneAsync(string name, List<string> sensorIds)
    {
        _name = name;
        _sensorIds = sensorIds ?? new List<string>();

        _logger.LogInformation("Water zone {ZoneId} registered with name {Name} and {SensorCount} sensors",
            this.GetPrimaryKeyString(), name, _sensorIds.Count);

        await UpdateZoneDataAsync();
    }

    public async Task<WaterZoneStatus> GetZoneStatusAsync()
    {
        if (_currentStatus == null)
        {
            await UpdateZoneDataAsync();
        }

        return _currentStatus ?? new WaterZoneStatus
        {
            ZoneId = this.GetPrimaryKeyString(),
            Name = _name,
            Status = ZoneStatus.Normal,
            LastUpdate = DateTime.UtcNow
        };
    }
    
    public Task<List<WaterAlert>> GetActiveAlertsAsync()
    {
        return Task.FromResult(_activeAlerts.ToList());
    }

    public async Task UpdateZoneDataAsync()
    {
        var sensorDataList = new List<WaterSensorData>();
        var allAlerts = new List<WaterAlert>();

        var tasks = _sensorIds.Select(async sensorId =>
        {
            try
            {
                var sensorGrain = GrainFactory.GetGrain<IWaterSensorGrain>(sensorId);

                if (await sensorGrain.IsActiveAsync())
                {
                    var sensorData = await sensorGrain.GetCurrentDataAsync();
                    var sensorAlerts = await sensorGrain.CheckAnomaliesAsync();
                    return (sensorData, sensorAlerts);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get data from sensor {SensorId} in zone {ZoneId}", sensorId, this.GetPrimaryKeyString());
            }
            return ((WaterSensorData?)null, (List<WaterAlert>?)null);
        });

        var results = await Task.WhenAll(tasks);

        foreach (var (sensorData, sensorAlerts) in results)
        {
            if (sensorData != null)
            {
                sensorDataList.Add(sensorData);
            }
            if (sensorAlerts != null)
            {
                allAlerts.AddRange(sensorAlerts);
            }
        }
        
        // Update active alerts
        _activeAlerts = allAlerts;

        foreach (var alert in allAlerts)
        {
            await _rabbitMQService.PublishAlertAsync(alert);
        }

        // Calculate zone metrics
        var zoneStatus = CalculateZoneStatus(sensorDataList);

        _currentStatus = zoneStatus;
        _lastUpdate = DateTime.UtcNow;

        await _redisService.SetAsync($"zone_status_{this.GetPrimaryKeyString()}", zoneStatus);

        await _mongoService.SaveZoneStatusAsync(zoneStatus);

        _logger.LogDebug("Zone {ZoneId} status updated with {SensorCount} sensors",
            this.GetPrimaryKeyString(), sensorDataList.Count);
    }
    
    //calculate zone data
    private WaterZoneStatus CalculateZoneStatus(List<WaterSensorData> sensorDataList)
    {
        if (!sensorDataList.Any())
        {
            return new WaterZoneStatus
            {
                ZoneId = this.GetPrimaryKeyString(),
                Name = _name,
                Status = ZoneStatus.Maintenance,
                ActiveSensors = new List<string>(),
                ActiveAlerts = _activeAlerts,
                LastUpdate = DateTime.UtcNow
            };
        }

        var totalFlowRate = sensorDataList.Sum(s => s.FlowRate);
        var averagePressure = sensorDataList.Average(s => s.Pressure);
        var waterQualityIndex = CalculateWaterQualityIndex(sensorDataList);
        var activeSensors = sensorDataList.Select(s => s.SensorId).ToList();

        var zoneStatus = DetermineZoneStatus(sensorDataList, averagePressure, waterQualityIndex);

        return new WaterZoneStatus
        {
            ZoneId = this.GetPrimaryKeyString(),
            Name = _name,
            TotalFlowRate = totalFlowRate,
            AveragePressure = averagePressure,
            WaterQualityIndex = waterQualityIndex,
            ActiveSensors = activeSensors,
            ActiveAlerts = _activeAlerts,
            LastUpdate = DateTime.UtcNow,
            Status = zoneStatus
        };
    }

    //determine the zone status(Normal,LowPressure,HighPressure,QualityIssue,LeakDetected,Maintenance)
    private ZoneStatus DetermineZoneStatus(List<WaterSensorData> sensorDataList, double averagePressure, double waterQualityIndex)
    {
        // Check for critical alerts first
        var criticalAlerts = _activeAlerts.Where(a => a.Severity == AlertSeverity.Critical).ToList();

        if (criticalAlerts.Any(a => a.Type == AlertType.LeakDetection))
            return ZoneStatus.LeakDetected;

        if (averagePressure < 30)
            return ZoneStatus.LowPressure;
        else if (averagePressure > 100)
            return ZoneStatus.HighPressure;

        if (waterQualityIndex < 60)
            return ZoneStatus.QualityIssue;

        if (DetectPotentialLeak(sensorDataList))
            return ZoneStatus.LeakDetected;

        return ZoneStatus.Normal;
    }

    //calculate quality index
    private double CalculateWaterQualityIndex(List<WaterSensorData> sensorDataList)
    {
        var qualityScores = new List<double>();

        foreach (var sensor in sensorDataList)
        {
            var pHScore = CalculatepHScore(sensor.pH);
            var turbidityScore = CalculateTurbidityScore(sensor.TurbidityNTU);
            var temperatureScore = CalculateTemperatureScore(sensor.Temperature);

            var sensorQualityScore = (pHScore + turbidityScore + temperatureScore) / 3.0;
            qualityScores.Add(sensorQualityScore);
        }

        return qualityScores.Any() ? qualityScores.Average() : 0.0;
    }
    
    private double CalculatepHScore(double pH)
    {
        if (pH >= 6.5 && pH <= 8.5)
            return 100.0;
        else if (pH >= 6.0 && pH <= 9.0)
            return 80.0;
        else if (pH >= 5.5 && pH <= 9.5)
            return 60.0;
        else
            return 40.0;
    }

    private double CalculateTurbidityScore(double turbidity)
    {
        if (turbidity <= 1.0)
            return 100.0;
        else if (turbidity <= 4.0)
            return 80.0;
        else if (turbidity <= 10.0)
            return 60.0;
        else
            return 40.0;
    }

    private double CalculateTemperatureScore(double temperature)
    {
        if (temperature >= 10 && temperature <= 20)
            return 100.0;
        else if (temperature >= 5 && temperature <= 25)
            return 80.0;
        else if (temperature >= 0 && temperature <= 30)
            return 60.0;
        else
            return 40.0;
    }

    private bool DetectPotentialLeak(List<WaterSensorData> sensorDataList)
    {
        var flowMeters = sensorDataList.Where(s => s.Type == SensorType.FlowMeter || s.Type == SensorType.Mixed).ToList();

        if (flowMeters.Count < 2)
            return false;

        var maxFlow = flowMeters.Max(s => s.FlowRate);
        var minFlow = flowMeters.Min(s => s.FlowRate);

        var flowDifference = maxFlow - minFlow;
        var averageFlow = flowMeters.Average(s => s.FlowRate);

        if (flowDifference > (averageFlow * 0.5))
        {
            _logger.LogWarning("Potential leak detected in zone {ZoneId}. Flow difference: {FlowDifference:F2} L/min",
                this.GetPrimaryKeyString(), flowDifference);
            return true;
        }

        return false;
    }

}