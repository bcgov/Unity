using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Applicant-profile-specific contact query service. Retrieves contacts linked to applicant profiles,
/// application-level contacts matched by OIDC subject, and applicant agent contacts derived from
/// the submission login token. Profile contacts are resolved by looking up form submissions that
/// match the OIDC subject to obtain applicant IDs, then querying <see cref="Contacts.ContactLink"/>
/// records against those IDs. When a single applicant ID is resolved the contacts are editable;
/// when multiple IDs are found the contacts are read-only. This service operates independently from the
/// generic <see cref="Contacts.ContactAppService"/> and queries repositories directly.
/// </summary>
public class ApplicantContactQueryService(
    IContactRepository contactRepository,
    IContactLinkRepository contactLinkRepository,
    IRepository<ApplicationFormSubmission, Guid> applicationFormSubmissionRepository,
    IRepository<ApplicationContact, Guid> applicationContactRepository,
    IRepository<ApplicantAgent, Guid> applicantAgentRepository,
    IRepository<Application, Guid> applicationRepository)
    : IApplicantContactQueryService, ITransientDependency
{
    private const string ApplicantEntityType = "Applicant";

    /// <inheritdoc />
    public async Task<List<ContactInfoItemDto>> GetApplicantContactsAsync(string subject)
    {
        var contactLinksQuery = await contactLinkRepository.GetQueryableAsync();
        var contactsQuery = await contactRepository.GetQueryableAsync();
        var submissionsQuery = await applicationFormSubmissionRepository.GetQueryableAsync();

        var applicantIds = await submissionsQuery
            .Where(s => s.OidcSub == subject)
            .Select(s => s.ApplicantId)
            .Distinct()
            .ToListAsync();

        var isEditable = applicantIds.Count <= 1;

        return await (
            from link in contactLinksQuery
            join contact in contactsQuery on link.ContactId equals contact.Id
            where link.RelatedEntityType == ApplicantEntityType
                  && applicantIds.Contains(link.RelatedEntityId)
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
                IsEditable = isEditable,
                ReferenceNo = null,
                CreationTime = contact.CreationTime
            }).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<ContactInfoItemDto>> GetApplicationContactsBySubjectAsync(string subject)
    {
        var submissionsQuery = await applicationFormSubmissionRepository.GetQueryableAsync();
        var applicationContactsQuery = await applicationContactRepository.GetQueryableAsync();
        var applicationsQuery = await applicationRepository.GetQueryableAsync();

        var applicationContacts = await (
            from submission in submissionsQuery
            join appContact in applicationContactsQuery on submission.ApplicationId equals appContact.ApplicationId
            join application in applicationsQuery on submission.ApplicationId equals application.Id
            where submission.OidcSub == subject
            select new ContactInfoItemDto
            {
                ContactId = appContact.Id,
                Name = appContact.ContactFullName,
                Title = appContact.ContactTitle,
                Email = appContact.ContactEmail,
                MobilePhoneNumber = appContact.ContactMobilePhone,
                WorkPhoneNumber = appContact.ContactWorkPhone,
                Role = GetMatchingRole(appContact.ContactType),
                ContactType = "Application",
                IsPrimary = false,
                IsEditable = false,
                ApplicationId = appContact.ApplicationId,
                ReferenceNo = application.ReferenceNo,
                CreationTime = appContact.CreationTime
            }).ToListAsync();

        return applicationContacts;
    }

    /// <inheritdoc />
    public async Task<List<ContactInfoItemDto>> GetApplicantAgentContactsBySubjectAsync(string subject)
    {
        var submissionsQuery = await applicationFormSubmissionRepository.GetQueryableAsync();
        var agentsQuery = await applicantAgentRepository.GetQueryableAsync();
        var applicationsQuery = await applicationRepository.GetQueryableAsync();

        var agentContacts = await (
            from submission in submissionsQuery
            join agent in agentsQuery on submission.ApplicationId equals agent.ApplicationId
            join application in applicationsQuery on submission.ApplicationId equals application.Id
            where submission.OidcSub == subject
            select new ContactInfoItemDto
            {
                ContactId = agent.Id,
                Name = agent.Name,
                Title = agent.Title,
                Email = agent.Email,
                WorkPhoneNumber = agent.Phone,
                WorkPhoneExtension = agent.PhoneExtension,
                MobilePhoneNumber = agent.Phone2,
                Role = agent.RoleForApplicant,
                ContactType = "ApplicantAgent",
                IsPrimary = false,
                IsEditable = false,
                ApplicationId = agent.ApplicationId,
                ReferenceNo = application.ReferenceNo,
                CreationTime = agent.CreationTime
            }).ToListAsync();

        return agentContacts;
    }

    /// <inheritdoc />
    public async Task<ApplicantContactInfoDto> GetByApplicantIdAsync(Guid applicantId)
    {
        var dto = new ApplicantContactInfoDto { Contacts = [] };
        if (applicantId == Guid.Empty)
        {
            return dto;
        }

        dto.Contacts.AddRange(await GetApplicantLinkedContactsAsync([applicantId], isEditable: true));
        dto.Contacts.AddRange(await GetApplicationContactsByApplicantIdAsync(applicantId));
        dto.Contacts.AddRange(await GetApplicantAgentContactsByApplicantIdAsync(applicantId));

        ResolvePrimary(dto.Contacts);

        return dto;
    }

    private async Task<List<ContactInfoItemDto>> GetApplicantLinkedContactsAsync(
        IReadOnlyCollection<Guid> applicantIds,
        bool isEditable)
    {
        if (applicantIds.Count == 0)
        {
            return [];
        }

        var contactLinksQuery = await contactLinkRepository.GetQueryableAsync();
        var contactsQuery = await contactRepository.GetQueryableAsync();

        return await (
            from link in contactLinksQuery
            join contact in contactsQuery on link.ContactId equals contact.Id
            where link.RelatedEntityType == ApplicantEntityType
                  && applicantIds.Contains(link.RelatedEntityId)
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
                ContactType = ApplicantEntityType,
                Role = link.Role,
                IsPrimary = link.IsPrimary,
                IsEditable = isEditable,
                ReferenceNo = null,
                CreationTime = contact.CreationTime
            }).ToListAsync();
    }

    private async Task<List<ContactInfoItemDto>> GetApplicationContactsByApplicantIdAsync(Guid applicantId)
    {
        var applicationContactsQuery = await applicationContactRepository.GetQueryableAsync();
        var applicationsQuery = await applicationRepository.GetQueryableAsync();

        // Resolve this applicant's applications up-front so we can map ReferenceNo by ApplicationId
        // without relying on EF join translation (mirrors the ApplicantAddresses widget pattern).
        var applicationRefMap = await applicationsQuery
            .Where(a => a.ApplicantId == applicantId)
            .Select(a => new { a.Id, a.ReferenceNo })
            .ToDictionaryAsync(a => a.Id, a => a.ReferenceNo);

        if (applicationRefMap.Count == 0)
        {
            return [];
        }

        var applicationIds = applicationRefMap.Keys.ToList();

        var contacts = await applicationContactsQuery
            .Where(c => applicationIds.Contains(c.ApplicationId))
            .Select(appContact => new ContactInfoItemDto
            {
                ContactId = appContact.Id,
                Name = appContact.ContactFullName,
                Title = appContact.ContactTitle,
                Email = appContact.ContactEmail,
                MobilePhoneNumber = appContact.ContactMobilePhone,
                WorkPhoneNumber = appContact.ContactWorkPhone,
                Role = GetMatchingRole(appContact.ContactType),
                ContactType = "Application",
                IsPrimary = false,
                IsEditable = false,
                ApplicationId = appContact.ApplicationId,
                CreationTime = appContact.CreationTime
            }).ToListAsync();

        foreach (var contact in contacts)
        {
            if (contact.ApplicationId.HasValue
                && applicationRefMap.TryGetValue(contact.ApplicationId.Value, out var referenceNo))
            {
                contact.ReferenceNo = referenceNo;
            }
        }

        return contacts;
    }

    private async Task<List<ContactInfoItemDto>> GetApplicantAgentContactsByApplicantIdAsync(Guid applicantId)
    {
        var agentsQuery = await applicantAgentRepository.GetQueryableAsync();
        var applicationsQuery = await applicationRepository.GetQueryableAsync();

        var agents = await agentsQuery
            .Where(a => a.ApplicantId == applicantId)
            .Select(agent => new ContactInfoItemDto
            {
                ContactId = agent.Id,
                Name = agent.Name,
                Title = agent.Title,
                Email = agent.Email,
                WorkPhoneNumber = agent.Phone,
                WorkPhoneExtension = agent.PhoneExtension,
                MobilePhoneNumber = agent.Phone2,
                Role = agent.RoleForApplicant,
                ContactType = "ApplicantAgent",
                IsPrimary = false,
                IsEditable = false,
                ApplicationId = agent.ApplicationId,
                CreationTime = agent.CreationTime
            }).ToListAsync();

        if (agents.Count == 0)
        {
            return agents;
        }

        var applicationIds = agents
            .Where(a => a.ApplicationId.HasValue)
            .Select(a => a.ApplicationId!.Value)
            .Distinct()
            .ToList();

        if (applicationIds.Count == 0)
        {
            return agents;
        }

        var applicationRefMap = await applicationsQuery
            .Where(a => applicationIds.Contains(a.Id))
            .Select(a => new { a.Id, a.ReferenceNo })
            .ToDictionaryAsync(a => a.Id, a => a.ReferenceNo);

        foreach (var agent in agents)
        {
            if (agent.ApplicationId.HasValue
                && applicationRefMap.TryGetValue(agent.ApplicationId.Value, out var referenceNo))
            {
                agent.ReferenceNo = referenceNo;
            }
        }

        return agents;
    }

    /// <summary>
    /// Ensures exactly one contact is flagged as primary. If none are explicitly flagged,
    /// the most recently created contact is elected.
    /// </summary>
    private static void ResolvePrimary(List<ContactInfoItemDto> contacts)
    {
        if (contacts.Count == 0 || contacts.Any(c => c.IsPrimary))
        {
            return;
        }

        var latest = contacts
            .OrderByDescending(c => c.CreationTime)
            .First();
        latest.IsPrimary = true;
        latest.IsPrimaryInferred = true;
    }

    private static string GetMatchingRole(string contactType)
    {
        return ApplicationContactOptionList.ContactTypeList.TryGetValue(contactType, out string? value)
                    ? value : contactType;
    }
}
