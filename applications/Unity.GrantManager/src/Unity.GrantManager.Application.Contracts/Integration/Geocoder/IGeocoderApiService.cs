using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integration.Geocoder
{
    public interface IGeocoderApiService : IApplicationService
    {
        Task<AddressDetailsDto> GetAddressDetailsAsync(string address);
        Task<ElectoralDistrictDto> GetElectoralDistrictAsync(LocationCoordinates locationCoordinates);
        Task<EconomicRegionDto> GetEconomicRegionAsync(LocationCoordinates locationCoordinates);
        Task<RegionalDistrictDto> GetRegionalDistrictAsync(LocationCoordinates locationCoordinates);
    }
}
