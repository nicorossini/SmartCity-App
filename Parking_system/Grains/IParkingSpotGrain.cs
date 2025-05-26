using Orleans;
using System.Threading.Tasks;

namespace Parking_system.Interfaces;

public interface IParkingSpotGrain : IGrainWithStringKey
{
    Task<bool> IsOccupied();
    Task UpdateOccupancy(bool isOccupied);
}
