using Orleans;
using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using SmartCity.Interfaces;
using SmartCity.Services;

[Serializable]
public class SoilMoistureSensorState
{
    public float Moisture { get; set; }
}

public class SoilMoistureSensorGrain : Grain, ISoilMoistureSensorGrain
{
    private readonly IPersistentState<SoilMoistureSensorState> _state;
    private readonly IRedisCacheService _cache;

    public SoilMoistureSensorGrain(
        [PersistentState("moisture", "mongoStore")] IPersistentState<SoilMoistureSensorState> state,
        IRedisCacheService cache)
    {
        _state = state;
        _cache = cache;
    }

    public async Task SetMoisture(float value)
    {
        _state.State.Moisture = value;
        await _state.WriteStateAsync();
        await _cache.SetAsync($"temp:{this.GetPrimaryKeyString()}", value.ToString());
    }

    public Task<float> GetMoisture() => Task.FromResult(_state.State.Moisture);
}