namespace SmartCityParking.Models
{
    public class ParkingResponse
        {
            public int SpotId { get; set; }
            public string UserId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
        }
}
