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
        await _cache.SetAsync($"moisture:{this.GetPrimaryKeyString()}", value.ToString());
    }

    public async Task<float> GetMoisture()
    {
        var cachedMoisture = await _cache.GetAsync($"moisture:{this.GetPrimaryKeyString()}");
        if (cachedMoisture != null && float.TryParse(cachedMoisture, out float moist))
        {
            return moist;
        }

        var moisture = _state.State.Moisture;

        return moisture;
    }
}