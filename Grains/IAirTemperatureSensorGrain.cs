using Orleans;
using System.Threading.Tasks;

namespace SmartCity.Interfaces
{
    public interface IAirTemperatureSensorGrain : IGrainWithStringKey
    {
        Task SetTemperature(float value);
        Task<float> GetTemperature();
    }
}