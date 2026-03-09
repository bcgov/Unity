using System;
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
    public string DataType => "CONTACT_CREATE_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var contactId = Guid.Parse(payload.ContactId ?? throw new ArgumentException("contactId is required"));
        var innerData = payload.Data?.ToObject<ContactCreateData>()
                        ?? throw new ArgumentException("Contact data is required");

        // Idempotency: if the contact already exists, treat as success
        var existing = await contactRepository.FindAsync(contactId);
        if (existing != null)
        {
            logger.LogInformation("Contact {ContactId} already exists. Treating as idempotent success.", contactId);
            return "Contact already exists";
        }

        logger.LogInformation("Creating contact {ContactId} for profile {ProfileId}", contactId, payload.ProfileId);

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

        await contactRepository.InsertAsync(contact, autoSave: true);

        // Create a contact link to track the relationship and primary status
        var contactLink = new ContactLink
        {
            ContactId = contactId,
            RelatedEntityType = innerData.ContactType ?? "PORTAL",
            RelatedEntityId = Guid.Parse(payload.ProfileId ?? Guid.Empty.ToString()),
            Role = innerData.Role,
            IsPrimary = innerData.IsPrimary,
            IsActive = true
        };

        await contactLinkRepository.InsertAsync(contactLink, autoSave: true);

        logger.LogInformation("Contact {ContactId} created successfully", contactId);
        return "Contact created successfully";
    }
}
