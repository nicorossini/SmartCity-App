
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Parking_system.Interfaces;
using System.Threading.Tasks;

namespace Parking_system.Controllers;
[ApiController]
[Route("api/sensor")]
public class SensorController : ControllerBase
{
    private readonly IGrainFactory _grains;

    public SensorController(IGrainFactory grains)
    {
        _grains = grains;
    }

    [HttpPost("{spotId}/update")]
    public async Task<IActionResult> UpdateSensor(string spotId, [FromBody] bool isOccupied)
    {
        var spotGrain = _grains.GetGrain<IParkingSpotGrain>(spotId);
        await spotGrain.UpdateOccupancy(isOccupied);

        var lotGrain = _grains.GetGrain<IParkingLotGrain>("MainLot");
        await lotGrain.SpotUpdated(spotId, isOccupied);

        return Ok();
    }
    [HttpGet("{spotId}/status")]
    public async Task<IActionResult> GetSpotStatus(string spotId)
    {
        var spotGrain = _grains.GetGrain<IParkingSpotGrain>(spotId);
        var isOccupied = await spotGrain.IsOccupied();
        return Ok(new { SpotId = spotId, IsOccupied = isOccupied });
    }
}
