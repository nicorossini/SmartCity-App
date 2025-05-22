using Orleans;
using System.Threading.Tasks;
using SmartCity.Interfaces;
using SmartCity.DTOs;

public class WaterZoneGrain : Grain, IWaterZoneGrain
{
    public async Task CheckConditionsAndActivateSprinklers()
    {
        var zoneId = this.GetPrimaryKeyString();

        var tempGrain = GrainFactory.GetGrain<IAirTemperatureSensorGrain>(zoneId);
        var moistureGrain = GrainFactory.GetGrain<ISoilMoistureSensorGrain>(zoneId);
        var sprinklerGrain = GrainFactory.GetGrain<ISprinklerGrain>(zoneId);

        float temp = await tempGrain.GetTemperature();
        float moisture = await moistureGrain.GetMoisture();

        if (temp > 30 && moisture < 50)
            await sprinklerGrain.TurnOn();
        else
            await sprinklerGrain.TurnOff();
    }

    public async Task<ZoneStatusDto> GetZoneStatus()
    {
        var zoneId = this.GetPrimaryKeyString();
        var temp = await GrainFactory.GetGrain<IAirTemperatureSensorGrain>(zoneId).GetTemperature();
        var moisture = await GrainFactory.GetGrain<ISoilMoistureSensorGrain>(zoneId).GetMoisture();
        var isOn = await GrainFactory.GetGrain<ISprinklerGrain>(zoneId).IsOn();

        return new ZoneStatusDto { Temperature = temp, Moisture = moisture, SprinklersOn = isOn };
    }
}