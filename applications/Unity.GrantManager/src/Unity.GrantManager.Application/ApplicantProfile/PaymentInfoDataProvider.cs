using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.ApplicantProfile
{
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class PaymentInfoDataProvider : IApplicantProfileDataProvider, ITransientDependency
    {
        public string Key => ApplicantProfileKeys.PaymentInfo;

        public Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            return Task.FromResult<ApplicantProfileDataDto>(new ApplicantPaymentInfoDto());
        }
    }
}
