using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using SmartCityParking.Grains.Interfaces;

namespace SmartCityParking.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Reactive after JWT installation
    public class TrafficController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;
        private readonly ILogger<TrafficController> _logger;

        public TrafficController(IGrainFactory grainFactory, ILogger<TrafficController> logger)
        {
            _grainFactory = grainFactory;
            _logger = logger;
        }

        [HttpPost("sensors/{sensorId}/register")]
        public async Task<IActionResult> RegisterSensor(string sensorId, [FromBody] RegisterSensorRequest request)
        {
            try
            {
                var trafficManager = _grainFactory.GetGrain<ITrafficManagerGrain>(0);
                await trafficManager.RegisterSensorAsync(sensorId, request.Location);

                return Ok(new { Message = $"Sensor {sensorId} registered successfully", SensorId = sensorId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering sensor {SensorId}", sensorId);
                return StatusCode(500, new { Error = "Failed to register sensor" });
            }
        }

        [HttpGet("sensors/{sensorId}/data")]
        public async Task<IActionResult> GetSensorData(string sensorId)
        {
            try
            {
                var sensorGrain = _grainFactory.GetGrain<ITrafficSensorGrain>(sensorId);
                var sensorData = await sensorGrain.GetSensorDataAsync();

                return Ok(sensorData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sensor data for {SensorId}", sensorId);
                return StatusCode(500, new { Error = "Failed to get sensor data" });
            }
        }

        [HttpGet("congestion/top")]
        public async Task<IActionResult> GetMostCongestedAreas([FromQuery] int count = 5)
        {
            try
            {
                var trafficManager = _grainFactory.GetGrain<ITrafficManagerGrain>(0);
                var congestedAreas = await trafficManager.GetMostCongestedAreasAsync(count);

                return Ok(congestedAreas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting congested areas");
                return StatusCode(500, new { Error = "Failed to get congested areas" });
            }
        }

        [HttpGet("sensors/all")]
        public async Task<IActionResult> GetAllSensorData()
        {
            try
            {
                var trafficManager = _grainFactory.GetGrain<ITrafficManagerGrain>(0);
                var allData = await trafficManager.GetAllSensorDataAsync();

                return Ok(allData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all sensor data");
                return StatusCode(500, new { Error = "Failed to get sensor data" });
            }
        }

        [HttpGet("sensors/{sensorId}/congestion")]
        public async Task<IActionResult> CheckCongestion(string sensorId)
        {
            try
            {
                var trafficManager = _grainFactory.GetGrain<ITrafficManagerGrain>(0);
                var isCongested = await trafficManager.IsAreaCongestedAsync(sensorId);

                return Ok(new { SensorId = sensorId, IsCongested = isCongested });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking congestion for {SensorId}", sensorId);
                return StatusCode(500, new { Error = "Failed to check congestion" });
            }
        }

        [HttpPost("sensors/{sensorId}/simulate")]
        public async Task<IActionResult> SimulateTrafficUpdate(string sensorId, [FromBody] SimulateTrafficRequest request)
        {
            try
            {
                var sensorGrain = _grainFactory.GetGrain<ITrafficSensorGrain>(sensorId);
                await sensorGrain.UpdateVehicleCountAsync(request.VehicleCount);

                return Ok(new { Message = "Traffic data simulated successfully", SensorId = sensorId, VehicleCount = request.VehicleCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating traffic for {SensorId}", sensorId);
                return StatusCode(500, new { Error = "Failed to simulate traffic" });
            }
        }
    }
}