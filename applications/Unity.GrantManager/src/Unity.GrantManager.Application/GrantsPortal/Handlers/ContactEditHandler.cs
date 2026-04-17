using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.GrantsPortal.Messages.Commands;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class ContactEditHandler(
    IContactRepository contactRepository,
    IContactLinkRepository contactLinkRepository,
    ILogger<ContactEditHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    private const string ApplicantEntityType = "Applicant";

    public string DataType => "CONTACT_EDIT_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var contactId = Guid.Parse(payload.ContactId ?? throw new ArgumentException("contactId is required"));
        var innerData = payload.Data?.ToObject<ContactEditData>()
                        ?? throw new ArgumentException("Contact data is required");

        if (innerData.ApplicantId == Guid.Empty)
        {
            throw new ArgumentException("applicantId is required");
        }

        logger.LogInformation("Editing contact {ContactId} for profile {ProfileId}", contactId, payload.ProfileId);

        await UpdateContactAsync(contactId, innerData);
        await SyncContactLinkAsync(contactId, innerData);

        logger.LogInformation("Contact {ContactId} updated successfully", contactId);
        return "Contact updated successfully";
    }

    private async Task UpdateContactAsync(Guid contactId, ContactEditData data)
    {
        var contact = await contactRepository.GetAsync(contactId);

        contact.Name = data.Name;
        contact.Email = data.Email;
        contact.Title = data.Title;
        contact.HomePhoneNumber = data.HomePhoneNumber;
        contact.MobilePhoneNumber = data.MobilePhoneNumber;
        contact.WorkPhoneNumber = data.WorkPhoneNumber;
        contact.WorkPhoneExtension = data.WorkPhoneExtension;

        await contactRepository.UpdateAsync(contact);
    }

    private async Task SyncContactLinkAsync(Guid contactId, ContactEditData data)
    {
        var contactLinks = await contactLinkRepository.GetListAsync(
            cl => cl.RelatedEntityType == ApplicantEntityType
                  && cl.RelatedEntityId == data.ApplicantId
                  && cl.IsActive);

        if (data.IsPrimary)
        {
            await DemoteOtherPrimaryLinksAsync(contactLinks, contactId);
        }

        var targetLink = contactLinks.FirstOrDefault(cl => cl.ContactId == contactId);
        if (targetLink != null)
        {
            targetLink.IsPrimary = data.IsPrimary;
            targetLink.Role = data.Role;
            await contactLinkRepository.UpdateAsync(targetLink);
        }
    }

    private async Task DemoteOtherPrimaryLinksAsync(List<ContactLink> contactLinks, Guid contactId)
    {
        foreach (var stale in contactLinks.Where(cl => cl.IsPrimary && cl.ContactId != contactId))
        {
            stale.IsPrimary = false;
            await contactLinkRepository.UpdateAsync(stale);
        }
    }
}
