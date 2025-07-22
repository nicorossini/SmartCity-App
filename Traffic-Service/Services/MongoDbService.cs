using MongoDB.Driver;

namespace Traffic_Service.Services
{
    public class MongoDbService : IMongoDbService
    {
        private readonly IMongoCollection<dynamic> _trafficCollection;

        public MongoDbService(IMongoDatabase database)
        {
            _trafficCollection = database.GetCollection<dynamic>("traffic_records");
        }

        public async Task InsertTrafficRecordAsync(object record)
        {
            await _trafficCollection.InsertOneAsync(record);
        }

        public async Task<List<T>> GetTrafficHistoryAsync<T>(string sensorId, DateTime from, DateTime to)
        {
            var filter = Builders<dynamic>.Filter.And(
                Builders<dynamic>.Filter.Eq("SensorId", sensorId),
                Builders<dynamic>.Filter.Gte("Timestamp", from),
                Builders<dynamic>.Filter.Lte("Timestamp", to)
            );

            var results = await _trafficCollection.Find(filter).ToListAsync();
            return results.Cast<T>().ToList();
        }
    }
}
