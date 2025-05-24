using Traffic_Service.Interfaces;

namespace Traffic_Service.Services
{
    public interface IRabbitMQService
    {
        Task PublishTrafficEventAsync(TrafficEvent trafficEvent);
        Task StartConsumingAsync();
    }
}
