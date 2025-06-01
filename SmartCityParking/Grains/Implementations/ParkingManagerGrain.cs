using Orleans;
using SmartCityParking.Grains.Interfaces;
using SmartCityParking.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartCityParking.Grains.Implementations
{
public class ParkingManagerGrain : Grain, IParkingManagerGrain
    {
        private int _totalSpots = 0;
        private readonly Dictionary<string, int> _userToSpot = new();

        public async Task<int?> ReserveParkingSpotAsync(string userId)
        {
            // Check if user already has a spot
            if (_userToSpot.ContainsKey(userId))
                return null;

            // Find available spot
            for (int spotId = 1; spotId <= _totalSpots; spotId++)
            {
                var spotGrain = GrainFactory.GetGrain<IParkingSpotGrain>(spotId);
                if (!await spotGrain.IsOccupiedAsync())
                {
                    if (await spotGrain.ReserveAsync(userId))
                    {
                        _userToSpot[userId] = spotId;
                        return spotId;
                    }
                }
            }

            return null; // No available spots
        }

        public async Task<bool> ReleaseParkingSpotAsync(string userId)
        {
            if (!_userToSpot.TryGetValue(userId, out int spotId))
                return false;

            var spotGrain = GrainFactory.GetGrain<IParkingSpotGrain>(spotId);
            if (await spotGrain.ReleaseAsync(userId))
            {
                _userToSpot.Remove(userId);
                return true;
            }

            return false;
        }

        public async Task<ParkingStatus> GetParkingStatusAsync()
        {
            var status = new ParkingStatus
            {
                TotalSpots = _totalSpots
            };

            int availableSpots = 0;
            for (int spotId = 1; spotId <= _totalSpots; spotId++)
            {
                var spotGrain = GrainFactory.GetGrain<IParkingSpotGrain>(spotId);
                var isOccupied = await spotGrain.IsOccupiedAsync();
                var currentUser = await spotGrain.GetCurrentUserAsync();

                status.SpotUsers[spotId] = currentUser;
                if (!isOccupied)
                    availableSpots++;
            }

            status.AvailableSpots = availableSpots;
            return status;
        }

        public async Task InitializeAsync(int totalSpots)
        {
            _totalSpots = totalSpots;
            
            // Initialize all parking spots
            for (int spotId = 1; spotId <= totalSpots; spotId++)
            {
                var spotGrain = GrainFactory.GetGrain<IParkingSpotGrain>(spotId);
                await spotGrain.InitializeAsync();
            }
        }
    }
}