using Orleans;

namespace Traffic_Service.Interfaces
{
    [GenerateSerializer]
    [Alias("Traffic_Service.Interfaces.CongestionArea")]
    public class CongestionArea
    {
        [Id(0)]
        public string SensorId { get; set; } = string.Empty;

        [Id(1)]
        public string Location { get; set; } = string.Empty;

        [Id(2)]
        public int VehicleCount { get; set; }

        [Id(3)]
        public string CongestionLevel { get; set; } = string.Empty;

        [Id(4)]
        public DateTime Timestamp { get; set; }
    }
}
