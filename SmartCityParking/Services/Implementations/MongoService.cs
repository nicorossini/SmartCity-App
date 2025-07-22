using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using SmartCityParking.Models;
using SmartCityParking.Services.Interfaces;

namespace SmartCityParking.Services.Implementations
{
    public class MongoService : IMongoService
        {
            private readonly IMongoCollection<ParkingEvent> _parkingEvents;

            public MongoService(IConfiguration configuration)
            {
                var connectionString = configuration.GetConnectionString("MongoDB");
                var mongoClient = new MongoClient(connectionString);
                var database = mongoClient.GetDatabase("SmartCityParking");
                _parkingEvents = database.GetCollection<ParkingEvent>("ParkingEvents");
            }

            public async Task LogParkingEventAsync(ParkingEvent parkingEvent)
            {
                await _parkingEvents.InsertOneAsync(parkingEvent);
            }

            public async Task<List<ParkingEvent>> GetParkingHistoryAsync(string? userId = null)
            {
                FilterDefinition<ParkingEvent> filter = Builders<ParkingEvent>.Filter.Empty;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    filter = Builders<ParkingEvent>.Filter.Eq(x => x.UserId, userId);
                }

                return await _parkingEvents
                    .Find(filter)
                    .SortByDescending(x => x.Timestamp)
                    .ToListAsync();
            }
        }
}