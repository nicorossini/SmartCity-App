using Orleans;
using Orleans.Concurrency;
using Orleans.CodeGeneration;
using System.Collections.Generic;

namespace SmartCityParking.Grains.Interfaces
{
    [GenerateSerializer]
    public class ParkingStatus
    {
        [Id(0)]
        public int TotalSpots { get; set; }
        
        [Id(1)]
        public int AvailableSpots { get; set; }
        
        [Id(2)]
        public Dictionary<int, string?> SpotUsers { get; set; } = new();
    }
}
