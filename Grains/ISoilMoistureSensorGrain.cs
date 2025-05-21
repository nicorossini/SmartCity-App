using Orleans;
using System.Threading.Tasks;

namespace SmartCity.Interfaces
{
    public interface ISoilMoistureSensorGrain : IGrainWithStringKey
    {
        Task SetMoisture(float value);
        Task<float> GetMoisture();
    }
}