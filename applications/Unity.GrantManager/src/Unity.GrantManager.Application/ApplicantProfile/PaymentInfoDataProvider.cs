using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Provides payment information for the applicant profile.
    /// This is a placeholder provider for future implementation.
    /// </summary>
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class PaymentInfoDataProvider : IApplicantProfileDataProvider, ITransientDependency
    {
        /// <inheritdoc />
        public string Key => ApplicantProfileKeys.PaymentInfo;

        /// <inheritdoc />
        public Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            return Task.FromResult<ApplicantProfileDataDto>(new ApplicantPaymentInfoDto());
        }
    }
}
