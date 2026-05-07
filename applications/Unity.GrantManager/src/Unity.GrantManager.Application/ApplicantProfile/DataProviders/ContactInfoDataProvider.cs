using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Provides contact information for the applicant profile by aggregating
    /// applicant linked contacts, application-level contacts, and applicant agent contacts.
    /// </summary>
    [ExposeServices(typeof(IApplicantProfileDataProvider))]
    public class ContactInfoDataProvider(
        ICurrentTenant currentTenant,
        IApplicantContactQueryService applicantContactQueryService)
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
                var applicantContacts = await applicantContactQueryService.GetApplicantContactsAsync(normalizedSubject);
                dto.Contacts.AddRange(applicantContacts);

                var applicationContacts = await applicantContactQueryService.GetApplicationContactsBySubjectAsync(normalizedSubject);
                dto.Contacts.AddRange(applicationContacts);

                var agentContacts = await applicantContactQueryService.GetApplicantAgentContactsBySubjectAsync(normalizedSubject);
                dto.Contacts.AddRange(agentContacts);
            }

            if (dto.Contacts.Count > 0 && !dto.Contacts.Any(c => c.IsPrimary))
            {
                var latest = dto.Contacts
                    .OrderByDescending(c => c.CreationTime)
                    .First();
                latest.IsPrimary = true;
                latest.IsPrimaryInferred = true;
            }

            return dto;
        }
    }
}
