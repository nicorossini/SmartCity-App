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
    private readonly ILogger<WaterDistributionController> _logger;

    public WaterDistributionController(IClusterClient clusterClient, ILogger<WaterDistributionController> logger)
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
            var currentSensor = await sensorGrain.RegisterSensorAsync(request.Location, request.Type, request.ZoneId);
            if (currentSensor)
                return Ok(new { Message = "Sensor registered successfully", SensorId = sensorId });
            else
                return Ok(new { Message = "Sensor already exists", SensorId = sensorId });
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

    // <summary>
    /// active sensor
    /// </summary>
    /// <param name="sensorId">Sensor identifier</param>
    [HttpGet("sensors/{sensorId}/active")]
    [ProducesResponseType(typeof(WaterSensorData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActiveSensor(string sensorId)
    {
        try
        {
            var sensorGrain = _clusterClient.GetGrain<IWaterSensorGrain>(sensorId);
            var isActive = await sensorGrain.IsActiveAsync();

            if (isActive)
            {
                return Ok(new { Error = "Sensor is already activated", SensorId = sensorId });
            }
            else
            {
                await sensorGrain.ActiveSensorAsync();
                return Ok(new { Error = "Sensor activated", SensorId = sensorId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sensor {sensorId} not found", sensorId);
            return NotFound(new { Error = "Sensor not found", SensorId = sensorId });
        }
    }

    // <summary>
    /// deactive sensor
    /// </summary>
    /// <param name="sensorId">Sensor identifier</param>
    [HttpGet("sensors/{sensorId}/deactive")]
    [ProducesResponseType(typeof(WaterSensorData), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactiveSensor(string sensorId)
    {
        try
        {
            var sensorGrain = _clusterClient.GetGrain<IWaterSensorGrain>(sensorId);
            var isActive = await sensorGrain.IsActiveAsync();

            if (isActive)
            {
                await sensorGrain.DeactivateSensorAsync();
                return Ok(new { Error = "Sensor deactivated", SensorId = sensorId });
            }
            else
            {
                return Ok(new { Error = "Sensor is already deactivated", SensorId = sensorId });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sensor {sensorId} not found", sensorId);
            return NotFound(new { Error = "Sensor not found", SensorId = sensorId });
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
            _logger.LogWarning("Number of zones {Count}: ", zones.Count);

            var allSensorsData = new Dictionary<string, WaterSensorData>();

            foreach (var zone in zones)
            {
                _logger.LogInformation("Zone {ZoneId} has {Count} active sensors", zone.ZoneId, zone.ActiveSensors.Count);

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