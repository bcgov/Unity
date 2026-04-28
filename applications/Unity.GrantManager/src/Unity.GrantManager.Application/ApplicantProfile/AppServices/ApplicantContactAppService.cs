using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Contacts;
using Unity.Modules.Shared;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Authorized facade over <see cref="IApplicantContactQueryService"/> (reads) and
/// <see cref="IContactManager"/> (writes) for the internal Applicant Contacts widget.
/// All writes are scoped to <c>RelatedEntityType = "Applicant"</c>.
/// </summary>
[Authorize]
public class ApplicantContactAppService(
    IApplicantContactQueryService applicantContactQueryService,
    IContactManager contactManager)
    : GrantManagerAppService, IApplicantContactAppService
{
    private const string ApplicantEntityType = "Applicant";

    /// <inheritdoc />
    [Authorize(UnitySelector.Applicant.Contact.Default)]
    public virtual Task<ApplicantContactInfoDto> GetByApplicantIdAsync(Guid applicantId)
    {
        return applicantContactQueryService.GetByApplicantIdAsync(applicantId);
    }

    /// <inheritdoc />
    [Authorize(UnitySelector.Applicant.Contact.Update)]
    public virtual async Task<ContactDto> UpdateAsync(Guid applicantId, Guid contactId, UpdateApplicantContactDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var (contact, link) = await contactManager.UpdateAsync(
            ApplicantEntityType,
            applicantId,
            contactId,
            new ContactInput(
                input.Name,
                input.Title,
                input.Email,
                HomePhoneNumber: null,
                input.MobilePhoneNumber,
                input.WorkPhoneNumber,
                input.WorkPhoneExtension),
            input.Role,
            input.IsPrimary);

        return new ContactDto
        {
            ContactId = contact.Id,
            Name = contact.Name,
            Title = contact.Title,
            Email = contact.Email,
            HomePhoneNumber = contact.HomePhoneNumber,
            MobilePhoneNumber = contact.MobilePhoneNumber,
            WorkPhoneNumber = contact.WorkPhoneNumber,
            WorkPhoneExtension = contact.WorkPhoneExtension,
            Role = link.Role,
            IsPrimary = link.IsPrimary
        };
    }

    /// <inheritdoc />
    [Authorize(UnitySelector.Applicant.Contact.Update)]
    public virtual async Task<bool> SetPrimaryAsync(Guid applicantId, Guid contactId)
    {
        await contactManager.SetPrimaryAsync(ApplicantEntityType, applicantId, contactId);
        return true;
    }
}
