using SmartCity.Interfaces;
using SmartCity.Interfaces.Models;

namespace SmartCity.Services
{
    public interface IRabbitMQService
    {
        Task PublishAlertAsync(WaterAlert waterAlertEvent);
        
    }
}