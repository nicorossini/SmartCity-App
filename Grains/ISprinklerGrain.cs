using Orleans;
using System.Threading.Tasks;

namespace SmartCity.Interfaces
{
    public interface ISprinklerGrain : IGrainWithStringKey
    {
        Task TurnOn();
        Task TurnOff();
        Task<bool> IsOn();
    }
}