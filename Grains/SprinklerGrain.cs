using Orleans;
using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using SmartCity.Interfaces;
using SmartCity.Services;

[Serializable]
public class SprinklerState
{
    public bool IsOn { get; set; }
}

public class SprinklerGrain : Grain, ISprinklerGrain
{
    private readonly IPersistentState<SprinklerState> _state;
    private readonly IRedisCacheService _cache;

    public SprinklerGrain(
        [PersistentState("sprinkler", "mongoStore")] IPersistentState<SprinklerState> state,
        IRedisCacheService cache)
    {
        _state = state;
        _cache = cache;
    }

    public async Task TurnOn()
    {
        _state.State.IsOn = true;
        await _state.WriteStateAsync();
        await _cache.SetAsync($"sprinkler:{this.GetPrimaryKeyString()}", "on");
    }

    public async Task TurnOff()
    {
        _state.State.IsOn = false;
        await _state.WriteStateAsync();
        await _cache.SetAsync($"sprinkler:{this.GetPrimaryKeyString()}", "off");
    }

    public Task<bool> IsOn() => Task.FromResult(_state.State.IsOn);
}