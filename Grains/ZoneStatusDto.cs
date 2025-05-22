using Orleans;

namespace SmartCity.DTOs
{
    [GenerateSerializer]
    public class ZoneStatusDto
    {
        [Id(0)]
        public string? ZoneId { get; set; }
        [Id(1)]
        public float Temperature { get; set; }
        [Id(2)]
        public float Moisture { get; set; }
        [Id(3)]
        public bool SprinklersOn { get; set; }
    }
}