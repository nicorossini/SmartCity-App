using Orleans;
using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using Parking_system.Interfaces;


public class ParkingLotGrain : Grain, IParkingLotGrain
{
    private HashSet<string> _spots = new();
    private Dictionary<string, bool> _spotStates = new();

    public Task RegisterSpot(string spotId)
    {
        _spots.Add(spotId);
        _spotStates[spotId] = false;
        return Task.CompletedTask;
    }

    public Task SpotUpdated(string spotId, bool isOccupied)
    {
        if (_spots.Contains(spotId))
            _spotStates[spotId] = isOccupied;

        return Task.CompletedTask;
    }

    public Task<int> GetAvailableSpots()
    {
        var available = _spotStates.Values.Count(v => !v);
        return Task.FromResult(available);
    }
}
