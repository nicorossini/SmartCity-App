using Orleans;
using SmartCityParking.Grains.Interfaces;
using SmartCityParking.Models;
using SmartCityParking.Services.Interfaces;

namespace SmartCityParking.Grains.Implementations
{
    public class ParkingSpotGrain : Grain, IParkingSpotGrain
    {
        private bool _isOccupied = false;
        private string? _currentUser = null;
        private readonly IMongoService _mongoService;

        public ParkingSpotGrain(IMongoService mongoService)
        {
            _mongoService = mongoService;
        }

        public Task<bool> IsOccupiedAsync()
        {
            return Task.FromResult(_isOccupied);
        }

        public async Task<bool> ReserveAsync(string userId)
        {
            if (_isOccupied)
                return false;

            _isOccupied = true;
            _currentUser = userId;

            // Log to MongoDB
            await _mongoService.LogParkingEventAsync(new ParkingEvent
            {
                UserId = userId,
                SpotId = (int)this.GetPrimaryKeyLong(),
                Action = "RESERVE",
                Timestamp = DateTime.UtcNow
            });

            return true;
        }

        public async Task<bool> ReleaseAsync(string userId)
        {
            if (!_isOccupied || _currentUser != userId)
                return false;

            _isOccupied = false;
            _currentUser = null;

            // Log to MongoDB
            await _mongoService.LogParkingEventAsync(new ParkingEvent
            {
                UserId = userId,
                SpotId = (int)this.GetPrimaryKeyLong(),
                Action = "RELEASE",
                Timestamp = DateTime.UtcNow
            });

            return true;
        }

        public Task<string?> GetCurrentUserAsync()
        {
            return Task.FromResult(_currentUser);
        }

        public Task InitializeAsync()
        {
            _isOccupied = false;
            _currentUser = null;
            return Task.CompletedTask;
        }
    }
}