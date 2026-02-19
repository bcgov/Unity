using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Provides contact information for the applicant profile by aggregating
    /// profile-linked contacts and application-level contacts.
    /// </summary>
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class ContactInfoDataProvider(
        ICurrentTenant currentTenant,
        IApplicantProfileContactService applicantProfileContactService)
        : IApplicantProfileDataProvider, ITransientDependency
    {
        /// <inheritdoc />
        public string Key => ApplicantProfileKeys.ContactInfo;

        /// <inheritdoc />
        public async Task<ApplicantProfileDataDto> GetDataAsync(ApplicantProfileInfoRequest request)
        {
            var dto = new ApplicantContactInfoDto
            {
                Contacts = []
            };

            var tenantId = request.TenantId;

            using (currentTenant.Change(tenantId))
            {
                var profileContacts = await applicantProfileContactService.GetProfileContactsAsync(request.ProfileId);
                dto.Contacts.AddRange(profileContacts);

                var applicationContacts = await applicantProfileContactService.GetApplicationContactsBySubjectAsync(request.Subject);
                dto.Contacts.AddRange(applicationContacts);
            }

            return dto;
        }
    }
}
