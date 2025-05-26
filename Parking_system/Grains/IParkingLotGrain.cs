using Orleans;
using System.Threading.Tasks;

namespace Parking_system.Interfaces;

public interface IParkingLotGrain : IGrainWithStringKey
{
    Task<int> GetAvailableSpots();
    Task RegisterSpot(string spotId);
    Task SpotUpdated(string spotId, bool isOccupied);
}
