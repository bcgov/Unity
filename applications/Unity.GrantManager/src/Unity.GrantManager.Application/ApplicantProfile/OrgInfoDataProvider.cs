using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.ApplicantProfile
{
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class OrgInfoDataProvider : IApplicantProfileDataProvider, ITransientDependency
    {
        public string Key => ApplicantProfileKeys.OrgInfo;

        public Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            return Task.FromResult<ApplicantProfileDataDto>(new ApplicantOrgInfoDto());
        }
    }
}
