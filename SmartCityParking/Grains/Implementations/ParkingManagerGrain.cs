
using Orleans;
using SmartCityParking.Grains.Interfaces;
using SmartCityParking.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartCityParking.Grains.Implementations
{
    public class ParkingManagerGrain : Grain, IParkingManagerGrain
    {
        private int _totalSpots = 0;
        private readonly Dictionary<string, List<int>> _userToSpots = new(); // Now tracks multiple spots per user
        private const int MaxSpotsPerUser = 2; // Maximum spots a user can reserve

        public async Task<int?> ReserveParkingSpotAsync(string userId)
        {
            // Check if user already has maximum allowed spots
            if (_userToSpots.TryGetValue(userId, out var userSpots) && userSpots.Count >= MaxSpotsPerUser)
                return null;

            // Find available spot
            for (int spotId = 1; spotId <= _totalSpots; spotId++)
            {
                var spotGrain = GrainFactory.GetGrain<IParkingSpotGrain>(spotId);
                if (!await spotGrain.IsOccupiedAsync())
                {
                    if (await spotGrain.ReserveAsync(userId))
                    {
                        // Initialize list if this is user's first reservation
                        if (!_userToSpots.ContainsKey(userId))
                        {
                            _userToSpots[userId] = new List<int>();
                        }
                        
                        _userToSpots[userId].Add(spotId);
                        return spotId;
                    }
                }
            }

            return null; // No available spots
        }

        public async Task<bool> ReleaseParkingSpotAsync(string userId)
        {
            if (!_userToSpots.TryGetValue(userId, out var userSpots) || userSpots.Count == 0)
                return false;

            // Get the last reserved spot (LIFO behavior)
            var lastSpotId = userSpots.Last();
            var spotGrain = GrainFactory.GetGrain<IParkingSpotGrain>(lastSpotId);
            
            if (await spotGrain.ReleaseAsync(userId))
            {
                userSpots.Remove(lastSpotId);
                
                // Remove user entry if no more spots reserved
                if (userSpots.Count == 0)
                {
                    _userToSpots.Remove(userId);
                }
                
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
        public Task<List<int>> GetUserReservedSpotsAsync(string userId)
        {
            if (_userToSpots.TryGetValue(userId, out var spots))
            {
                return Task.FromResult(new List<int>(spots)); // Return copy of the list
            }
            return Task.FromResult(new List<int>());
        }
    }
}
