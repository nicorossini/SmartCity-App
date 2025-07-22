using Orleans;

namespace Traffic_Service.Interfaces
{
    [GenerateSerializer]
    [Alias("Traffic_Service.Interfaces.TrafficSensorData")]
    public class TrafficSensorData
    {
        [Id(0)]
        public string SensorId { get; set; } = string.Empty;

        [Id(1)]
        public string Location { get; set; } = string.Empty;

        [Id(2)]
        public int VehicleCount { get; set; }

        [Id(3)]
        public DateTime LastUpdated { get; set; }

        [Id(4)]
        public bool IsActive { get; set; }
    }
}
