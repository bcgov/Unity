using System.Threading.Tasks;
using Unity.GrantManager.Integration.Geocoder;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations.Geocoder
{
    public interface IGeocoderApiService : IApplicationService
    {
        Task<AddressDetailsDto> GetAddressDetailsAsync(string address);
        Task<ElectoralDistrictDto> GetElectoralDistrictAsync(LocationCoordinates locationCoordinates);
        Task<EconomicRegionDto> GetEconomicRegionAsync(LocationCoordinates locationCoordinates);
        Task<RegionalDistrictDto> GetRegionalDistrictAsync(LocationCoordinates locationCoordinates);
    }
}
