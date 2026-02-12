using System.Threading.Tasks;
using Unity.GrantManager.Applicants.ProfileData;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Applicants.ApplicantProfile
{
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class OrgInfoDataProvider : IApplicantProfileDataProvider, ITransientDependency
    {
        public string Key => ApplicantProfileKeys.OrgInfo;

        public Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            // TODO: Implement organization info retrieval
            return Task.FromResult<ApplicantProfileDataDto>(new ApplicantOrgInfoDto());
        }
    }
}
