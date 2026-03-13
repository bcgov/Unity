using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Provides contact information for the applicant profile by aggregating
    /// profile-linked contacts, application-level contacts, and applicant agent contacts.
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

            var normalizedSubject = SubjectNormalizer.Normalize(request.Subject);
            if (normalizedSubject is null) return dto;

            var tenantId = request.TenantId;

            using (currentTenant.Change(tenantId))
            {
                var profileContacts = await applicantProfileContactService.GetProfileContactsAsync(request.ProfileId);
                dto.Contacts.AddRange(profileContacts);

                var applicationContacts = await applicantProfileContactService.GetApplicationContactsBySubjectAsync(normalizedSubject);
                dto.Contacts.AddRange(applicationContacts);

                var agentContacts = await applicantProfileContactService.GetApplicantAgentContactsBySubjectAsync(normalizedSubject);
                dto.Contacts.AddRange(agentContacts);
            }

            return dto;
        }
    }
}
