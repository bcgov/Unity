using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.GrantsPortal.Messages.Commands;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class ContactCreateHandler(
    IContactRepository contactRepository,
    IContactLinkRepository contactLinkRepository,
    ILogger<ContactCreateHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    private const string ApplicantEntityType = "Applicant";

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

        await contactRepository.InsertAsync(contact);

        // Demote existing primary contact links for the same applicant
        if (innerData.IsPrimary)
        {
            var contactLinks = await contactLinkRepository.GetListAsync(
                cl => cl.RelatedEntityType == ApplicantEntityType
                      && cl.RelatedEntityId == innerData.ApplicantId
                      && cl.IsActive);

            foreach (var stale in contactLinks.Where(cl => cl.IsPrimary))
            {
                stale.IsPrimary = false;
                await contactLinkRepository.UpdateAsync(stale);
            }
        }

        // Create a contact link to track the relationship and primary status
        var contactLink = new ContactLink
        {
            ContactId = contactId,
            RelatedEntityType = ApplicantEntityType,
            RelatedEntityId = innerData.ApplicantId,
            Role = innerData.Role,
            IsPrimary = innerData.IsPrimary,
            IsActive = true
        };

        await contactLinkRepository.InsertAsync(contactLink);

        logger.LogInformation("Contact {ContactId} created successfully", contactId);
        return "Contact created successfully";
    }
}
