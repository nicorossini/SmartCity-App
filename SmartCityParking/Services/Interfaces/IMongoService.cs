using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using SmartCityParking.Models;

namespace SmartCityParking.Services.Interfaces
{
    public interface IMongoService
        {
            Task LogParkingEventAsync(ParkingEvent parkingEvent);
            Task<List<ParkingEvent>> GetParkingHistoryAsync(string? userId = null);
        }
}