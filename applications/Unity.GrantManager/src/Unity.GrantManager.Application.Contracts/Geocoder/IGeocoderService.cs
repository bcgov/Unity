using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Geocoder
{
    public interface IGeocoderService : IApplicationService
    {
        Task<AddressDetailsDto> GetAddressDetails(string address);
    }
}
