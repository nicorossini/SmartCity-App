using Orleans;
using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using SmartCity.Interfaces;
using SmartCity.Services;

[Serializable]
public class AirTemperatureSensorState
{
    public float Temperature { get; set; }
}

public class AirTemperatureSensorGrain : Grain, IAirTemperatureSensorGrain
{
    private readonly IPersistentState<AirTemperatureSensorState> _state;
    private readonly IRedisCacheService _cache;

    public AirTemperatureSensorGrain(
        [PersistentState("airTemp", "mongoStore")] IPersistentState<AirTemperatureSensorState> state,
        IRedisCacheService cache)
    {
        _state = state;
        _cache = cache;
    }

    public async Task SetTemperature(float value)
    {
        _state.State.Temperature = value;
        await _state.WriteStateAsync();
        await _cache.SetAsync($"temp:{this.GetPrimaryKeyString()}", value.ToString());
    }

    public Task<float> GetTemperature() => Task.FromResult(_state.State.Temperature);
}