using MongoDB.Driver;
using SmartCity.Interfaces.Models;

namespace SmartCity.Services;

public class MongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<WaterSensorData> _sensorDataCollection;
    private readonly IMongoCollection<WaterZoneStatus> _zoneStatusCollection;
    private readonly IMongoCollection<WaterAlert> _alertsCollection;

    public MongoDbService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MongoDB") 
            ?? "mongodb://localhost:27017";
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("WaterDistributionDB");
        
        _sensorDataCollection = _database.GetCollection<WaterSensorData>("water_sensor_data");
        _zoneStatusCollection = _database.GetCollection<WaterZoneStatus>("water_zone_status");
        _alertsCollection = _database.GetCollection<WaterAlert>("water_alerts");
    }

    public async Task SaveSensorDataAsync(WaterSensorData data)
    {
        await _sensorDataCollection.InsertOneAsync(data);
    }

    public async Task SaveZoneStatusAsync(WaterZoneStatus status)
    {
        await _zoneStatusCollection.ReplaceOneAsync(
            z => z.ZoneId == status.ZoneId,
            status,
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task SaveAlertAsync(WaterAlert alert)
    {
        await _alertsCollection.InsertOneAsync(alert);
    }

    public async Task<List<WaterSensorData>> GetSensorHistoryAsync(string sensorId, DateTime from, DateTime to)
    {
        return await _sensorDataCollection
            .Find(s => s.SensorId == sensorId && s.Timestamp >= from && s.Timestamp <= to)
            .SortByDescending(s => s.Timestamp)
            .ToListAsync();
    }
}