using Orleans;

namespace SmartCityParking.Grains.Interfaces
{
    [GenerateSerializer]
    [Alias("SmartCityParking.Grains.Interfaces.TrafficEvent")]
    public class TrafficEvent
    {
        [Id(0)]
        public string SensorId { get; set; } = string.Empty;

        [Id(1)]
        public int VehicleCount { get; set; }

        [Id(2)]
        public DateTime Timestamp { get; set; }

        [Id(3)]
        public string EventType { get; set; } = string.Empty;
    }
}
