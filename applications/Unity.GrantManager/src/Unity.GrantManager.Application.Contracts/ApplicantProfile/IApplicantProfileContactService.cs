using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Provides applicant-profile-specific contact retrieval operations.
/// This service aggregates contacts from two sources: profile-linked contacts
/// and application-level contacts matched by OIDC subject.
/// </summary>
public interface IApplicantProfileContactService
{
    /// <summary>
    /// Retrieves contacts linked to the specified applicant profile.
    /// </summary>
    /// <param name="profileId">The unique identifier of the applicant profile.</param>
    /// <returns>A list of <see cref="ContactInfoItemDto"/> with <c>IsEditable</c> set to <c>true</c>.</returns>
    Task<List<ContactInfoItemDto>> GetProfileContactsAsync(Guid profileId);

    /// <summary>
    /// Retrieves application contacts associated with submissions matching the given OIDC subject.
    /// The subject is normalized by stripping the domain portion (after <c>@</c>) and converting to upper case.
    /// </summary>
    /// <param name="subject">The OIDC subject identifier (e.g. "user@idir").</param>
    /// <returns>A list of <see cref="ContactInfoItemDto"/> with <c>IsEditable</c> set to <c>false</c>.</returns>
    Task<List<ContactInfoItemDto>> GetApplicationContactsBySubjectAsync(string subject);
}
