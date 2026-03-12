using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.GrantsPortal.Messages.Commands;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class ContactCreateHandler(
    IContactRepository contactRepository,
    IContactLinkRepository contactLinkRepository,
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicantAgentRepository applicantAgentRepository,
    ILogger<ContactCreateHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "CONTACT_CREATE_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var contactId = Guid.Parse(payload.ContactId ?? throw new ArgumentException("contactId is required"));
        var profileId = Guid.Parse(payload.ProfileId ?? throw new ArgumentException("profileId is required"));
        var innerData = payload.Data?.ToObject<ContactCreateData>()
                        ?? throw new ArgumentException("Contact data is required");

        // Idempotency: if the contact already exists, treat as success
        var existing = await contactRepository.FindAsync(contactId);
        if (existing != null)
        {
            logger.LogInformation("Contact {ContactId} already exists. Treating as idempotent success.", contactId);
            return "Contact already exists";
        }

        logger.LogInformation("Creating contact {ContactId} for profile {ProfileId}", contactId, profileId);

        var contact = new Contact
        {
            Name = innerData.Name,
            Email = innerData.Email,
            Title = innerData.Title,
            HomePhoneNumber = innerData.HomePhoneNumber,
            MobilePhoneNumber = innerData.MobilePhoneNumber,
            WorkPhoneNumber = innerData.WorkPhoneNumber,
            WorkPhoneExtension = innerData.WorkPhoneExtension
        };

        EntityHelper.TrySetId(contact, () => contactId);

        // Lookup applicant agent IDs associated with this subject's submissions
        var applicantAgentIds = await GetApplicantAgentIdsAsync(payload.Subject);
        if (applicantAgentIds.Count > 0)
        {
            contact.SetProperty("applicantAgentIds", applicantAgentIds);
            logger.LogInformation("Found {Count} applicant agent(s) for subject {Subject}", applicantAgentIds.Count, payload.Subject);
        }

        await contactRepository.InsertAsync(contact);

        // Create a contact link to track the relationship and primary status
        var contactLink = new ContactLink
        {
            ContactId = contactId,
            RelatedEntityType = innerData.ContactType ?? "PORTAL",
            RelatedEntityId = profileId,
            Role = innerData.Role,
            IsPrimary = innerData.IsPrimary,
            IsActive = true
        };

        await contactLinkRepository.InsertAsync(contactLink);

        logger.LogInformation("Contact {ContactId} created successfully", contactId);
        return "Contact created successfully";
    }

    private async Task<List<string>> GetApplicantAgentIdsAsync(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return [];
        }

        var normalizedSub = NormalizeOidcSub(subject);
        if (string.IsNullOrWhiteSpace(normalizedSub))
        {
            return [];
        }

        // Find submissions matching the normalized OidcSub
        var submissions = await applicationFormSubmissionRepository.GetListAsync(s => s.OidcSub == normalizedSub);
        if (submissions.Count == 0)
        {
            logger.LogDebug("No submissions found for subject {Subject} (normalized: {NormalizedSub})", subject, normalizedSub);
            return [];
        }

        // Get distinct application IDs from the submissions
        var applicationIds = submissions
            .Select(s => s.ApplicationId)
            .Distinct()
            .ToList();

        // Lookup applicant agents linked to those applications
        var agents = await applicantAgentRepository
            .GetListAsync(a => a.ApplicationId != null && applicationIds.Contains(a.ApplicationId!.Value));

        return [.. agents
            .Select(a => a.Id.ToString())
            .Distinct()];
    }

    /// <summary>
    /// Normalizes a raw OIDC subject by stripping the IDP suffix (after @) and uppercasing.
    /// This matches the format stored in ApplicationFormSubmission.OidcSub.
    /// </summary>
    internal static string? NormalizeOidcSub(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var atIndex = subject.IndexOf('@');

        if (atIndex == 0)
        {
            return null;
        }

        return atIndex > 0
            ? subject[..atIndex].ToUpperInvariant()
            : subject.ToUpperInvariant();
    }
}
