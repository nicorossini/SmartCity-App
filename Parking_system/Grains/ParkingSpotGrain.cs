using Orleans;
using System;
using System.Threading.Tasks;
using Orleans.Runtime;
using Parking_system.Interfaces;



public class ParkingSpotGrain : Grain, IParkingSpotGrain
{
    private bool _isOccupied;

    public Task<bool> IsOccupied() => Task.FromResult(_isOccupied);

    public Task UpdateOccupancy(bool isOccupied)
    {
        _isOccupied = isOccupied;
        // Optionally notify ParkingLotGrain or log
        return Task.CompletedTask;
    }
}
