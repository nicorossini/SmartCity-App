using SmartCity.Interfaces;
using SmartCity.Interfaces.Models;
using System.Data;
using Orleans;

namespace SmartCity.Grains;


public class WaterManagerGrain : Grain, IWaterManagerGrain
{
    private readonly ILogger<WaterManagerGrain> _logger;
    private readonly List<string> _registeredZones = new();
    private readonly Dictionary<string, string> _sensorToZoneMapping = new();

    public WaterManagerGrain(
        ILogger<WaterManagerGrain> logger)
    {
        _logger = logger;
    }

    public async Task<List<WaterZoneStatus>> GetAllZonesStatusAsync()
    {
        var zoneStatuses = new List<WaterZoneStatus>();

        foreach (var zoneId in _registeredZones)
        {
            try
            {
                var zoneGrain = GrainFactory.GetGrain<IWaterZoneGrain>(zoneId);
                var status = await zoneGrain.GetZoneStatusAsync();
                zoneStatuses.Add(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status for zone {ZoneId}", zoneId);
            }
        }

        return zoneStatuses;
    }

    public async Task<List<WaterAlert>> GetCriticalAlertsAsync()
    {
        var criticalAlerts = new List<WaterAlert>();

        foreach (var zoneId in _registeredZones)
        {
            try
            {
                var zoneGrain = GrainFactory.GetGrain<IWaterZoneGrain>(zoneId);
                var alerts = await zoneGrain.GetActiveAlertsAsync();

                // Filter for critical and emergency alerts
                var criticalZoneAlerts = alerts.Where(a =>
                    a.Severity == AlertSeverity.Critical ||
                    a.Severity == AlertSeverity.Emergency).ToList();

                criticalAlerts.AddRange(criticalZoneAlerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get alerts for zone {ZoneId}", zoneId);
            }
        }

        return criticalAlerts.OrderByDescending(a => a.Timestamp).ToList();
    }

    public async Task<WaterSensorData> GetSensorDataAsync(string sensorId)
    {
        try
        {
            var sensorGrain = GrainFactory.GetGrain<IWaterSensorGrain>(sensorId);
            return await sensorGrain.GetCurrentDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data for sensor {SensorId}", sensorId);
            throw;
        }
    }

    public async Task<Dictionary<string, double>> GetSystemOverviewAsync()
    {
        var overview = new Dictionary<string, double>
        {
            ["TotalZones"] = _registeredZones.Count,
            ["TotalSensors"] = _sensorToZoneMapping.Count,
            ["TotalFlowRate"] = 0,
            ["AverageSystemPressure"] = 0,
            ["AverageWaterQuality"] = 0,
            ["ActiveCriticalAlerts"] = 0,
            ["ZonesInNormalStatus"] = 0,
            ["ZonesWithIssues"] = 0
        };

        var zoneStatuses = await GetAllZonesStatusAsync();

        if (zoneStatuses.Any())
        {
            overview["TotalFlowRate"] = zoneStatuses.Sum(z => z.TotalFlowRate);
            overview["AverageSystemPressure"] = zoneStatuses.Average(z => z.AveragePressure);
            overview["AverageWaterQuality"] = zoneStatuses.Average(z => z.WaterQualityIndex);
            overview["ZonesInNormalStatus"] = zoneStatuses.Count(z => z.Status == ZoneStatus.Normal);
            overview["ZonesWithIssues"] = zoneStatuses.Count(z => z.Status != ZoneStatus.Normal);
        }

        var criticalAlerts = await GetCriticalAlertsAsync();
        overview["ActiveCriticalAlerts"] = criticalAlerts.Count;

        return overview;
    }

    public async Task InitializeTestDataAsync()
    {
        _logger.LogInformation("Initializing test data for water distribution system...");

        _registeredZones.Clear();
        _sensorToZoneMapping.Clear();

        var testZones = new List<(string zoneId, string name, List<string> sensorIds)>
        {
            ("zone_downtown", "Downtown District", new List<string> { "sensor_dt_001", "sensor_dt_002", "sensor_dt_003" }),
            ("zone_residential", "Residential Area", new List<string> { "sensor_res_001", "sensor_res_002", "sensor_res_003", "sensor_res_004" }),
            ("zone_industrial", "Industrial Zone", new List<string> { "sensor_ind_001", "sensor_ind_002" }),
            ("zone_suburban", "Suburban Area", new List<string> { "sensor_sub_001", "sensor_sub_002", "sensor_sub_003" })
        };

        foreach (var (zoneId, name, sensorIds) in testZones)
        {
            _registeredZones.Add(zoneId);

            foreach (var sensorId in sensorIds)
            {
                _sensorToZoneMapping[sensorId] = zoneId;

                var sensorGrain = GrainFactory.GetGrain<IWaterSensorGrain>(sensorId);
                var sensorType = DetermineSensorType(sensorId);
                var location = GenerateLocationName(zoneId, sensorId);

                await sensorGrain.RegisterSensorAsync(location, sensorType, zoneId);
            }

            
            var zoneGrain = GrainFactory.GetGrain<IWaterZoneGrain>(zoneId);
            await zoneGrain.RegisterZoneAsync(name, sensorIds);
        }

        _logger.LogInformation("Test data initialization completed. Created {ZoneCount} zones with {SensorCount} sensors",
        _registeredZones.Count, _sensorToZoneMapping.Count);
    }

    private SensorType DetermineSensorType(string sensorId)
    {
        if (sensorId.Contains("dt")) // Downtown - mixed sensors
            return SensorType.Mixed;
        else if (sensorId.Contains("res")) // Residential - flow meters
            return SensorType.FlowMeter;
        else if (sensorId.Contains("ind")) // Industrial - pressure sensors
            return SensorType.PressureSensor;
        else if (sensorId.Contains("sub")) // Suburban - quality sensors
            return SensorType.QualitySensor;
        else
            return SensorType.Mixed;
    }

    private string GenerateLocationName(string zoneId, string sensorId)
    {
        var zoneNames = new Dictionary<string, string>
        {
            ["zone_downtown"] = "Downtown",
            ["zone_residential"] = "Residential",
            ["zone_industrial"] = "Industrial",
            ["zone_suburban"] = "Suburban"
        };

        var zoneName = zoneNames.GetValueOrDefault(zoneId, "Unknown");
        var sensorNumber = sensorId.Split('_').LastOrDefault() ?? "001";

        return $"{zoneName} Sensor {sensorNumber}";
    }

    public async Task RegisterZoneAsync(string zoneId, string name, List<string> sensorIds)
    {
        if (!_registeredZones.Contains(zoneId))
        {
            _registeredZones.Add(zoneId);

            foreach (var sensorId in sensorIds)
            {
                _sensorToZoneMapping[sensorId] = zoneId;
            }

            var zoneGrain = GrainFactory.GetGrain<IWaterZoneGrain>(zoneId);
            await zoneGrain.RegisterZoneAsync(name, sensorIds);

            _logger.LogInformation("Zone {ZoneId} registered with {SensorCount} sensors", zoneId, sensorIds.Count);
        }
    }
    
}