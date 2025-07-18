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
        private DateTime? _reservationTime;

        public ParkingSpotGrain(IMongoService mongoService)
        {
            _mongoService = mongoService;
        }

        public Task<bool> IsOccupiedAsync() => Task.FromResult(_isOccupied);

        public async Task<bool> ReserveAsync(string userId)
        {
            if (_isOccupied)
                return false;

            _isOccupied = true;
            _currentUser = userId;
            _reservationTime = DateTime.UtcNow;

            await _mongoService.LogParkingEventAsync(new ParkingEvent
            {
                UserId = userId,
                SpotId = (int)this.GetPrimaryKeyLong(),
                Action = "RESERVE",
                Timestamp = _reservationTime.Value
            });

            return true;
        }

        public async Task<bool> ReleaseAsync(string userId)
        {
            if (!_isOccupied || _currentUser != userId)
                return false;

            var releaseTime = DateTime.UtcNow;
            
            _isOccupied = false;
            _currentUser = null;
            _reservationTime = null;

            await _mongoService.LogParkingEventAsync(new ParkingEvent
            {
                UserId = userId,
                SpotId = (int)this.GetPrimaryKeyLong(),
                Action = "RELEASE",
                Timestamp = releaseTime
            });

            return true;
        }

        public Task<string?> GetCurrentUserAsync() => Task.FromResult(_currentUser);
        public Task<DateTime?> GetReservationTimeAsync() => Task.FromResult(_reservationTime);
        public Task InitializeAsync() => Task.CompletedTask;
    }
}