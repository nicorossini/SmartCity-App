using Orleans;
using Traffic_Service.Interfaces;
using Traffic_Service.Services;

namespace Traffic_Service.Grains
{
    namespace SmartCity.TrafficService.Grains
    {
        public class TrafficSensorGrain : Grain, ITrafficSensorGrain
        {
            private readonly ILogger<TrafficSensorGrain> _logger;
            private readonly IRabbitMQService _rabbitMQService;
            private readonly IRedisService _redisService;

            private TrafficSensorData _sensorData = new();
            private IDisposable? _timer;
            private readonly Random _random = new();

            public TrafficSensorGrain(
                ILogger<TrafficSensorGrain> logger,
                IRabbitMQService rabbitMQService,
                IRedisService redisService)
            {
                _logger = logger;
                _rabbitMQService = rabbitMQService;
                _redisService = redisService;
            }

            public override Task OnActivateAsync(CancellationToken cancellationToken)
            {
                _sensorData.SensorId = this.GetPrimaryKeyString();
                _sensorData.Location = $"Intersection_{_sensorData.SensorId}";
                _sensorData.IsActive = true;
                _sensorData.LastUpdated = DateTime.UtcNow;
                _sensorData.VehicleCount = 0;

                _logger.LogInformation("Traffic sensor {SensorId} activated at {Location}",
                    _sensorData.SensorId, _sensorData.Location);

                return base.OnActivateAsync(cancellationToken);
            }

            public Task<int> GetCurrentVehicleCountAsync()
            {
                return Task.FromResult(_sensorData.VehicleCount);
            }

            public async Task UpdateVehicleCountAsync(int count)
            {
                _sensorData.VehicleCount = count;
                _sensorData.LastUpdated = DateTime.UtcNow;

                // Update Redis cache
                await _redisService.SetAsync($"traffic_sensor:{_sensorData.SensorId}", _sensorData);

                // Notify traffic manager
                var trafficManager = GrainFactory.GetGrain<ITrafficManagerGrain>(0);
                await trafficManager.ProcessTrafficUpdateAsync(_sensorData.SensorId, count);

                // Send event to RabbitMQ
                var trafficEvent = new TrafficEvent
                {
                    SensorId = _sensorData.SensorId,
                    VehicleCount = count,
                    Timestamp = DateTime.UtcNow,
                    EventType = "VehicleCountUpdate"
                };

                await _rabbitMQService.PublishTrafficEventAsync(trafficEvent);

                _logger.LogInformation("Sensor {SensorId} updated: {Count} vehicles",
                    _sensorData.SensorId, count);
            }

            public Task<TrafficSensorData> GetSensorDataAsync()
            {
                return Task.FromResult(_sensorData);
            }

            public Task StartPeriodicUpdatesAsync()
            {
                if (_timer != null)
                    return Task.CompletedTask;

                _timer = this.RegisterTimer(
                    SimulateTrafficData,
                    null,
                    TimeSpan.FromMinutes(1), // Start after 1 minute
                    TimeSpan.FromMinutes(5)  // Update every 5 minutes
                );

                _logger.LogInformation("Started periodic updates for sensor {SensorId}", _sensorData.SensorId);
                return Task.CompletedTask;
            }

            public Task StopPeriodicUpdatesAsync()
            {
                _timer?.Dispose();
                _timer = null;

                _logger.LogInformation("Stopped periodic updates for sensor {SensorId}", _sensorData.SensorId);
                return Task.CompletedTask;
            }

            private async Task SimulateTrafficData(object state)
            {
                // Simulate realistic traffic patterns based on time of day
                var hour = DateTime.Now.Hour;
                int baseCount = GetBaseTrafficForHour(hour);

                // Add some randomness (±20%)
                var variation = (int)(baseCount * 0.2);
                var actualCount = baseCount + _random.Next(-variation, variation + 1);
                actualCount = Math.Max(0, actualCount); // Ensure non-negative

                await UpdateVehicleCountAsync(actualCount);
            }

            private int GetBaseTrafficForHour(int hour)
            {
                // Simulate realistic traffic patterns
                return hour switch
                {
                    >= 6 and <= 9 => _random.Next(80, 120),   // Morning rush
                    >= 10 and <= 16 => _random.Next(30, 60),  // Mid-day
                    >= 17 and <= 19 => _random.Next(90, 130), // Evening rush
                    >= 20 and <= 23 => _random.Next(20, 40),  // Evening
                    _ => _random.Next(5, 15)                   // Night/early morning
                };
            }
        }
    }
}
