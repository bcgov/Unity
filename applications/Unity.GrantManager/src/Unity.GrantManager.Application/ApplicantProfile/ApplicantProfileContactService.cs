using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Contacts;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Applicant-profile-specific contact service. Retrieves contacts linked to applicant profiles
/// and application-level contacts matched by OIDC subject. This service operates independently
/// from the generic <see cref="Contacts.ContactAppService"/> and queries repositories directly.
/// </summary>
public class ApplicantProfileContactService(
    IContactRepository contactRepository,
    IContactLinkRepository contactLinkRepository,
    IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository,
    IRepository<ApplicationContact, Guid> applicationContactRepository)
    : IApplicantProfileContactService, ITransientDependency
{
    private const string ApplicantProfileEntityType = "ApplicantProfile";

    /// <inheritdoc />
    public async Task<List<ContactInfoItemDto>> GetProfileContactsAsync(Guid profileId)
    {
        var contactLinksQuery = await contactLinkRepository.GetQueryableAsync();
        var contactsQuery = await contactRepository.GetQueryableAsync();

        return await (
            from link in contactLinksQuery
            join contact in contactsQuery on link.ContactId equals contact.Id
            where link.RelatedEntityType == ApplicantProfileEntityType
                  && link.RelatedEntityId == profileId
                  && link.IsActive
            select new ContactInfoItemDto
            {
                ContactId = contact.Id,
                Name = contact.Name,
                Title = contact.Title,
                Email = contact.Email,
                HomePhoneNumber = contact.HomePhoneNumber,
                MobilePhoneNumber = contact.MobilePhoneNumber,
                WorkPhoneNumber = contact.WorkPhoneNumber,
                WorkPhoneExtension = contact.WorkPhoneExtension,
                ContactType = link.RelatedEntityType,
                Role = link.Role,
                IsPrimary = link.IsPrimary,
                IsEditable = true,
                ApplicationId = null
            }).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ContactInfoItemDto>> GetApplicationContactsBySubjectAsync(string subject)
    {
        var normalizedSubject = subject.Contains('@')
            ? subject[..subject.IndexOf('@')].ToUpperInvariant()
            : subject.ToUpperInvariant();

        var submissionsQuery = await applicationFormSubmissionRepository.GetQueryableAsync();
        var applicationContactsQuery = await applicationContactRepository.GetQueryableAsync();

        var applicationContacts = await (
            from submission in submissionsQuery
            join appContact in applicationContactsQuery on submission.ApplicationId equals appContact.ApplicationId
            where submission.OidcSub == normalizedSubject
            select new ContactInfoItemDto
            {
                ContactId = appContact.Id,
                Name = appContact.ContactFullName,
                Title = appContact.ContactTitle,
                Email = appContact.ContactEmail,
                MobilePhoneNumber = appContact.ContactMobilePhone,
                WorkPhoneNumber = appContact.ContactWorkPhone,
                Role = appContact.ContactType,
                ContactType = "Application",
                IsPrimary = false,
                IsEditable = false,
                ApplicationId = appContact.ApplicationId
            }).ToListAsync();

        return applicationContacts;
    }
}
