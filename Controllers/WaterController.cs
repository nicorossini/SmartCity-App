using Microsoft.AspNetCore.Mvc;
using Orleans;
using SmartCity.Interfaces;
using SmartCity.DTOs;
using System.Threading.Tasks;

namespace SmartCity.Controllers
{
    [ApiController]
    [Route("api/water")]
    public class WaterController : ControllerBase
    {
        private readonly IGrainFactory _grainFactory;

        public WaterController(IGrainFactory grainFactory)
        {
            _grainFactory = grainFactory;
        }

        [HttpPost("sensor/temp/{zoneId}")]
        public async Task<IActionResult> SetTemperature(string zoneId, [FromBody] float value)
        {
            var grain = _grainFactory.GetGrain<IAirTemperatureSensorGrain>(zoneId);
            await grain.SetTemperature(value);
            return Ok("Temperature updated.");
        }

        [HttpPost("sensor/moisture/{zoneId}")]
        public async Task<IActionResult> SetMoisture(string zoneId, [FromBody] float value)
        {
            var grain = _grainFactory.GetGrain<ISoilMoistureSensorGrain>(zoneId);
            await grain.SetMoisture(value);
            return Ok("Moisture updated.");

        }

        [HttpPost("zone/{zoneId}/simulate")]
        public async Task<IActionResult> Simulate(string zoneId)
        {
            var zoneGrain = _grainFactory.GetGrain<IWaterZoneGrain>(zoneId);
            await zoneGrain.CheckConditionsAndActivateSprinklers();
            return Ok("Simulation executed.");
        }

        [HttpGet("zone/{zoneId}/status")]
        public async Task<IActionResult> GetStatus(string zoneId)
        {
            var zoneGrain = _grainFactory.GetGrain<IWaterZoneGrain>(zoneId);
            var status = await zoneGrain.GetZoneStatus();
            return Ok(status);
        }
    }
}