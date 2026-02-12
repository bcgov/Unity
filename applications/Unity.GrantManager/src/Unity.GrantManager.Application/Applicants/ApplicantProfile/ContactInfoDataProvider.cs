using System.Threading.Tasks;
using Unity.GrantManager.Applicants.ProfileData;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Applicants.ApplicantProfile
{
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class ContactInfoDataProvider : IApplicantProfileDataProvider, ITransientDependency
    {
        public string Key => ApplicantProfileKeys.ContactInfo;

        public Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            // TODO: Implement contact info retrieval
            return Task.FromResult<ApplicantProfileDataDto>(new ApplicantContactInfoDto());
        }
    }
}
