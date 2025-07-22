using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace SmartCityParking.Models
{
    public class ParkingEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public int SpotId { get; set; }
        public string Action { get; set; } = string.Empty; // "RESERVE" or "RELEASE"
        public DateTime Timestamp { get; set; }
    }
}