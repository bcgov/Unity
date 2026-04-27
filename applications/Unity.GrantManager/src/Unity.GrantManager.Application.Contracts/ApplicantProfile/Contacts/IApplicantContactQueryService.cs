using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Provides applicant-profile-specific contact retrieval operations.
/// This query service aggregates contacts from three sources: profile-linked contacts,
/// application-level contacts matched by OIDC subject, and applicant agent
/// contacts derived from the submission login token.
/// </summary>
public interface IApplicantContactQueryService
{
    /// <summary>
    /// Retrieves contacts linked to the applicant profile by resolving applicant IDs from
    /// form submissions that match the given OIDC subject. When the subject resolves to a
    /// single applicant ID the returned contacts are editable; when multiple applicant IDs
    /// are found they are read-only.
    /// </summary>    
    /// <param name="subject">The pre-normalized OIDC subject identifier used to resolve applicant IDs from submissions.</param>
    /// <returns>A list of <see cref="ContactInfoItemDto"/> with <c>IsEditable</c> reflecting the applicant-count rule.</returns>
    Task<List<ContactInfoItemDto>> GetApplicantContactsAsync(string subject);

    /// <summary>
    /// Retrieves application contacts associated with submissions matching the given OIDC subject.
    /// The subject is normalized by stripping the domain portion (after <c>@</c>) and converting to upper case.
    /// </summary>
    /// <param name="subject">The OIDC subject identifier (e.g. "user@idir").</param>
    /// <returns>A list of <see cref="ContactInfoItemDto"/> with <c>IsEditable</c> set to <c>false</c>.</returns>
    Task<List<ContactInfoItemDto>> GetApplicationContactsBySubjectAsync(string subject);

    /// <summary>
    /// Retrieves contacts derived from applicant agents on applications whose form submissions
    /// match the given OIDC subject. The join path is Submission → Application → ApplicantAgent.
    /// The subject is normalized by stripping the domain portion (after <c>@</c>) and converting to upper case.
    /// </summary>
    /// <param name="subject">The OIDC subject identifier (e.g. "user@idir").</param>
    /// <returns>A list of <see cref="ContactInfoItemDto"/> with <c>IsEditable</c> set to <c>false</c>.</returns>
    Task<List<ContactInfoItemDto>> GetApplicantAgentContactsBySubjectAsync(string subject);

    /// <summary>
    /// Retrieves the aggregated contact info for the specified applicant, combining
    /// applicant-linked contacts (<see cref="Contacts.ContactLink"/> with
    /// <c>RelatedEntityType = "Applicant"</c>), application contacts, and applicant agent contacts
    /// for every application owned by the applicant. Applicant-linked contacts are marked editable;
    /// application and agent contacts are always read-only. The primary flag is resolved either from
    /// an explicit <c>IsPrimary</c> contact link or, if none exists, by falling back to the most
    /// recently created contact.
    /// </summary>
    /// <param name="applicantId">The unique identifier of the applicant.</param>
    /// <returns>A populated <see cref="ApplicantContactInfoDto"/>.</returns>
    Task<ApplicantContactInfoDto> GetByApplicantIdAsync(Guid applicantId);
}
