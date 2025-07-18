using SmartCityParking.Grains.Interfaces;

namespace SmartCityParking.Services.Interfaces
{
    public interface IRabbitMQService
    {
        Task PublishTrafficEventAsync(TrafficEvent trafficEvent);
        Task StartConsumingAsync();
    }
}
