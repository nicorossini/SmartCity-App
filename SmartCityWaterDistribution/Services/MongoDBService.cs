using MongoDB.Driver;
using SmartCity.Interfaces.Models;

namespace SmartCity.Services;

public class MongoDbService : IMongoDBService
{
    private readonly IMongoCollection<WaterSensorData> _sensorDataCollection;
    private readonly IMongoCollection<WaterZoneStatus> _zoneStatusCollection;
    private readonly IMongoCollection<WaterAlert> _alertsCollection;

    public MongoDbService(IMongoDatabase _database)
    {
        _sensorDataCollection = _database.GetCollection<WaterSensorData>("water_sensor_data");
        _zoneStatusCollection = _database.GetCollection<WaterZoneStatus>("water_zone_status");
        _alertsCollection = _database.GetCollection<WaterAlert>("water_alerts");
    }

    public async Task SaveSensorDataAsync(WaterSensorData data)
    {
        await _sensorDataCollection.InsertOneAsync(data);
    }

    public async Task<WaterZoneStatus?> GetZoneByIdAsync(string zoneId)
    {
        return await _zoneStatusCollection.Find(z => z.ZoneId == zoneId).FirstOrDefaultAsync();
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

    public async Task<bool> SensorExistsAsync(string sensorID, string zoneID)
    {
        var filter = Builders<WaterSensorData>.Filter.And(
            Builders<WaterSensorData>.Filter.Eq(s => s.SensorId, sensorID),
            Builders<WaterSensorData>.Filter.Eq(s => s.ZoneId, zoneID)
        );

        var exists = await _sensorDataCollection.Find(filter).AnyAsync();
        Console.WriteLine($"Sensor exists: {exists} for SensorId={sensorID}, ZoneId={zoneID}");
        return exists;
    }
   
}