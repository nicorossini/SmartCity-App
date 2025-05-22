using Orleans;
using System.Threading.Tasks;
using SmartCity.DTOs;
public interface IWaterZoneGrain : IGrainWithStringKey
{
    Task CheckConditionsAndActivateSprinklers();
    Task<ZoneStatusDto> GetZoneStatus();
}