using Orleans;
using Traffic_Service.Interfaces;
using Traffic_Service.Services;

namespace Traffic_Service.Grains
{
    public class TrafficManagerGrain : Grain, ITrafficManagerGrain
    {
        private readonly ILogger<TrafficManagerGrain> _logger;
        private readonly IMongoDbService _mongoDbService;
        private readonly IRedisService _redisService;
        private readonly IRabbitMQService _rabbitMQService;

        private readonly Dictionary<string, string> _registeredSensors = new();
        private readonly Dictionary<string, int> _currentTrafficData = new();
        private const int CONGESTION_THRESHOLD = 80; // vehicles per 5-minute period

        public TrafficManagerGrain(
            ILogger<TrafficManagerGrain> logger,
            IMongoDbService mongoDbService,
            IRedisService redisService,
            IRabbitMQService rabbitMQService)
        {
            _logger = logger;
            _mongoDbService = mongoDbService;
            _redisService = redisService;
            _rabbitMQService = rabbitMQService;
        }

        public async Task RegisterSensorAsync(string sensorId, string location)
        {
            _registeredSensors[sensorId] = location;
            _currentTrafficData[sensorId] = 0;

            // Initialize sensor grain and start periodic updates
            var sensorGrain = GrainFactory.GetGrain<ITrafficSensorGrain>(sensorId);
            await sensorGrain.StartPeriodicUpdatesAsync();

            _logger.LogInformation("Registered traffic sensor {SensorId} at {Location}", sensorId, location);
        }

        public async Task ProcessTrafficUpdateAsync(string sensorId, int vehicleCount)
        {
            _currentTrafficData[sensorId] = vehicleCount;

            // Update Redis with current data
            await _redisService.SetAsync("traffic_current_data", _currentTrafficData);

            // Store historical data in MongoDB
            var historicalRecord = new
            {
                SensorId = sensorId,
                Location = _registeredSensors.GetValueOrDefault(sensorId, "Unknown"),
                VehicleCount = vehicleCount,
                Timestamp = DateTime.UtcNow,
                IsCongested = vehicleCount >= CONGESTION_THRESHOLD
            };

            await _mongoDbService.InsertTrafficRecordAsync(historicalRecord);

            // Check for congestion and send alerts
            if (vehicleCount >= CONGESTION_THRESHOLD)
            {
                var congestionAlert = new TrafficEvent
                {
                    SensorId = sensorId,
                    VehicleCount = vehicleCount,
                    Timestamp = DateTime.UtcNow,
                    EventType = "CongestionAlert"
                };

                await _rabbitMQService.PublishTrafficEventAsync(congestionAlert);

                _logger.LogWarning("Congestion detected at sensor {SensorId}: {Count} vehicles",
                    sensorId, vehicleCount);
            }
        }

        public Task<List<CongestionArea>> GetMostCongestedAreasAsync(int topCount = 5)
        {
            var congestedAreas = _currentTrafficData
                .OrderByDescending(kvp => kvp.Value)
                .Take(topCount)
                .Select(kvp => new CongestionArea
                {
                    SensorId = kvp.Key,
                    Location = _registeredSensors.GetValueOrDefault(kvp.Key, "Unknown"),
                    VehicleCount = kvp.Value,
                    CongestionLevel = GetCongestionLevel(kvp.Value),
                    Timestamp = DateTime.UtcNow
                })
                .ToList();

            return Task.FromResult(congestedAreas);
        }

        public Task<Dictionary<string, int>> GetAllSensorDataAsync()
        {
            return Task.FromResult(new Dictionary<string, int>(_currentTrafficData));
        }

        public Task<bool> IsAreaCongestedAsync(string sensorId)
        {
            var vehicleCount = _currentTrafficData.GetValueOrDefault(sensorId, 0);
            return Task.FromResult(vehicleCount >= CONGESTION_THRESHOLD);
        }

        private string GetCongestionLevel(int vehicleCount)
        {
            return vehicleCount switch
            {
                >= 100 => "SEVERE",
                >= 80 => "HIGH",
                >= 50 => "MODERATE",
                >= 30 => "LOW",
                _ => "NONE"
            };
        }
    }
}
