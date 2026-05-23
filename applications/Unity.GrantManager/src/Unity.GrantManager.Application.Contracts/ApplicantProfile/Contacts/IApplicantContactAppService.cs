using System;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Contacts;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Authorized application service that exposes Create / Update / SetPrimary
/// operations for applicant-scoped contacts (those linked to the Applicant aggregate
/// via <see cref="ContactLink"/> with <c>RelatedEntityType = "Applicant"</c>),
/// and aggregated read access that combines applicant-linked, application, and
/// applicant-agent contacts into a single view model.
/// <para>
/// This is the HTTP-facing surface used by the internal Applicant Contacts widget.
/// The underlying <see cref="IContactAppService"/> remains unexposed so that existing
/// Applicant Portal message handlers (which write directly to the repositories) are
/// unaffected by authorization.
/// </para>
/// </summary>
public interface IApplicantContactAppService : IApplicationService
{
    /// <summary>
    /// Retrieves the aggregated contact info for the specified applicant.
    /// </summary>
    Task<ApplicantContactInfoDto> GetByApplicantIdAsync(Guid applicantId);

    /// <summary>
    /// Updates an existing applicant-linked contact.
    /// </summary>
    Task<ContactDto> UpdateAsync(Guid applicantId, Guid contactId, UpdateApplicantContactDto input);

    /// <summary>
    /// Flags the specified contact as primary for the given applicant.
    /// </summary>
    Task<bool> SetPrimaryAsync(Guid applicantId, Guid contactId);
}
