using System.Threading.Tasks;
using Unity.GrantManager.Applicants.ProfileData;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Applicants.ApplicantProfile
{
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class AddressInfoDataProvider : IApplicantProfileDataProvider, ITransientDependency
    {
        public string Key => ApplicantProfileKeys.AddressInfo;

        public Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            // TODO: Implement address info retrieval
            return Task.FromResult<ApplicantProfileDataDto>(new ApplicantAddressInfoDto());
        }
    }
}
