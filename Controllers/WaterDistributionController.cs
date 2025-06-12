using Microsoft.AspNetCore.Mvc;
using Orleans;
using SmartCity.Interfaces;
using SmartCity.Interfaces.Models;

namespace SmartCity.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WaterDistributionController : ControllerBase
{
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<WaterController> _logger;

    public WaterDistributionController(IClusterClient clusterClient, ILogger<WaterController> logger)
    {
        _clusterClient = clusterClient;
        _logger = logger;
    }

    /// <summary>
    /// Register a new water sensor
    /// </summary>
    /// <param name="sensorId">Unique sensor identifier</param>
    /// <param name="request">Sensor registration details</param>
    [HttpPost("sensors/{sensorId}/register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterSensor(string sensorId, [FromBody] RegisterSensorRequest request)
    {
        try
        {
            var sensorGrain = _clusterClient.GetGrain<IWaterSensorGrain>(sensorId);
            await sensorGrain.RegisterSensorAsync(request.Location, request.Type, request.ZoneId);
            
            _logger.LogInformation("Registered water sensor {SensorId} at {Location}", 
                sensorId, request.Location);
            
            return Ok(new { Message = "Sensor registered successfully", SensorId = sensorId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register sensor {SensorId}", sensorId);
            return BadRequest(new { Error = "Failed to register sensor", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get current data from a specific sensor
    /// </summary>
    /// <param name="sensorId">Sensor identifier</param>
    [HttpGet("sensors/{sensorId}/data")]
    [ProducesResponseType(typeof(WaterSensorData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSensorData(string sensorId)
    {
        try
        {
            var sensorGrain = _clusterClient.GetGrain<IWaterSensorGrain>(sensorId);
            var isActive = await sensorGrain.IsActiveAsync();
            
            if (!isActive)
            {
                return NotFound(new { Error = "Sensor not found or inactive", SensorId = sensorId });
            }
            
            var data = await sensorGrain.GetCurrentDataAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get data for sensor {SensorId}", sensorId);
            return NotFound(new { Error = "Sensor not found", SensorId = sensorId });
        }
    }

    /// <summary>
    /// Manually simulate sensor data (for testing)
    /// </summary>
    /// <param name="sensorId">Sensor identifier</param>
    /// <param name="data">Sensor data to simulate</param>
    [HttpPost("sensors/{sensorId}/simulate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SimulateSensorData(string sensorId, [FromBody] WaterSensorData data)
    {
        try
        {
            var sensorGrain = _clusterClient.GetGrain<IWaterSensorGrain>(sensorId);
            var isActive = await sensorGrain.IsActiveAsync();
            
            if (!isActive)
            {
                return NotFound(new { Error = "Sensor not found or inactive", SensorId = sensorId });
            }
            
            await sensorGrain.UpdateSensorDataAsync(data);
            
            _logger.LogInformation("Simulated data for sensor {SensorId}", sensorId);
            return Ok(new { Message = "Sensor data simulated successfully", SensorId = sensorId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate data for sensor {SensorId}", sensorId);
            return BadRequest(new { Error = "Failed to simulate sensor data", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get all active sensors data
    /// </summary>
    [HttpGet("sensors/all")]
    [ProducesResponseType(typeof(Dictionary<string, WaterSensorData>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSensorsData()
    {
        try
        {
            var managerGrain = _clusterClient.GetGrain<IWaterManagerGrain>("water-system");
            var zones = await managerGrain.GetAllZonesStatusAsync();
            
            var allSensorsData = new Dictionary<string, WaterSensorData>();
            
            foreach (var zone in zones)
            {
                foreach (var sensorId in zone.ActiveSensors)
                {
                    try
                    {
                        var sensorData = await managerGrain.GetSensorDataAsync(sensorId);
                        allSensorsData[sensorId] = sensorData;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("Failed to get data for sensor {SensorId}: {Error}", 
                            sensorId, ex.Message);
                    }
                }
            }
            
            return Ok(allSensorsData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all sensors data");
            return BadRequest(new { Error = "Failed to retrieve sensors data", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get status of a specific water distribution zone
    /// </summary>
    /// <param name="zoneId">Zone identifier</param>
    [HttpGet("zones/{zoneId}/status")]
    [ProducesResponseType(typeof(WaterZoneStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetZoneStatus(string zoneId)
    {
        try
        {
            var zoneGrain = _clusterClient.GetGrain<IWaterZoneGrain>(zoneId);
            var status = await zoneGrain.GetZoneStatusAsync();
            
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get status for zone {ZoneId}", zoneId);
            return NotFound(new { Error = "Zone not found", ZoneId = zoneId });
        }
    }

    /// <summary>
    /// Get active alerts for a specific zone
    /// </summary>
    /// <param name="zoneId">Zone identifier</param>
    [HttpGet("zones/{zoneId}/alerts")]
    [ProducesResponseType(typeof(List<WaterAlert>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetZoneAlerts(string zoneId)
    {
        try
        {
            var zoneGrain = _clusterClient.GetGrain<IWaterZoneGrain>(zoneId);
            var alerts = await zoneGrain.GetActiveAlertsAsync();
            
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get alerts for zone {ZoneId}", zoneId);
            return NotFound(new { Error = "Zone not found", ZoneId = zoneId });
        }
    }

    /// <summary>
    /// Add a sensor to a specific zone
    /// </summary>
    /// <param name="zoneId">Zone identifier</param>
    /// <param name="sensorId">Sensor identifier</param>
    [HttpPost("zones/{zoneId}/sensors/{sensorId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddSensorToZone(string zoneId, string sensorId)
    {
        try
        {
            var zoneGrain = _clusterClient.GetGrain<IWaterZoneGrain>(zoneId);
            await zoneGrain.AddSensorAsync(sensorId);
            
            _logger.LogInformation("Added sensor {SensorId} to zone {ZoneId}", sensorId, zoneId);
            return Ok(new { Message = "Sensor added to zone successfully", ZoneId = zoneId, SensorId = sensorId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add sensor {SensorId} to zone {ZoneId}", sensorId, zoneId);
            return BadRequest(new { Error = "Failed to add sensor to zone", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get all zones status
    /// </summary>
    [HttpGet("zones/all")]
    [ProducesResponseType(typeof(List<WaterZoneStatus>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllZones()
    {
        try
        {
            var managerGrain = _clusterClient.GetGrain<IWaterManagerGrain>("water-system");
            var zones = await managerGrain.GetAllZonesStatusAsync();
            
            return Ok(zones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all zones status");
            return BadRequest(new { Error = "Failed to retrieve zones status", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get system overview with key metrics
    /// </summary>
    [HttpGet("system/overview")]
    [ProducesResponseType(typeof(Dictionary<string, double>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemOverview()
    {
        try
        {
            var managerGrain = _clusterClient.GetGrain<IWaterManagerGrain>("water-system");
            var overview = await managerGrain.GetSystemOverviewAsync();
            
            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system overview");
            return BadRequest(new { Error = "Failed to retrieve system overview", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get critical alerts across the system
    /// </summary>
    [HttpGet("alerts/critical")]
    [ProducesResponseType(typeof(List<WaterAlert>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCriticalAlerts()
    {
        try
        {
            var managerGrain = _clusterClient.GetGrain<IWaterManagerGrain>("water-system");
            var alerts = await managerGrain.GetCriticalAlertsAsync();
            
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get critical alerts");
            return BadRequest(new { Error = "Failed to retrieve critical alerts", Details = ex.Message });
        }
    }

    /// <summary>
    /// Check for detected leaks across all zones
    /// </summary>
    [HttpGet("leaks/detected")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetectedLeaks()
    {
        try
        {
            var managerGrain = _clusterClient.GetGrain<IWaterManagerGrain>("water-system");
            var zones = await managerGrain.GetAllZonesStatusAsync();
            
            var leakZones = new List<string>();
            
            foreach (var zone in zones)
            {
                var zoneGrain = _clusterClient.GetGrain<IWaterZoneGrain>(zone.ZoneId);
                var hasLeak = await zoneGrain.IsLeakDetectedAsync();
                
                if (hasLeak)
                {
                    leakZones.Add(zone.ZoneId);
                }
            }
            
            return Ok(leakZones);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for leaks");
            return BadRequest(new { Error = "Failed to check for leaks", Details = ex.Message });
        }
    }

    /// <summary>
    /// Initialize test data (sensors and zones)
    /// </summary>
    [HttpPost("system/initialize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> InitializeTestData()
    {
        try
        {
            var managerGrain = _clusterClient.GetGrain<IWaterManagerGrain>("water-system");
            await managerGrain.InitializeTestDataAsync();
            
            _logger.LogInformation("Test data initialized successfully");
            return Ok(new { Message = "Test data initialized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize test data");
            return BadRequest(new { Error = "Failed to initialize test data", Details = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new 
        { 
            Status = "Healthy", 
            Service = "Water Distribution Service",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}

/// <summary>
/// Request model for sensor registration
/// </summary>
public record RegisterSensorRequest
{
    public string Location { get; init; } = string.Empty;
    public SensorType Type { get; init; }
    public string ZoneId { get; init; } = string.Empty;
}