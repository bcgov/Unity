using System;
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

        var contact = await contactRepository.GetAsync(contactId);

        contact.Name = innerData.Name;
        contact.Email = innerData.Email;
        contact.Title = innerData.Title;
        contact.HomePhoneNumber = innerData.HomePhoneNumber;
        contact.MobilePhoneNumber = innerData.MobilePhoneNumber;
        contact.WorkPhoneNumber = innerData.WorkPhoneNumber;
        contact.WorkPhoneExtension = innerData.WorkPhoneExtension;

        // Sync contact-link primary flags to match the incoming value
        var contactLinks = await contactLinkRepository.GetListAsync(
            cl => cl.RelatedEntityType == ApplicantEntityType
                  && cl.RelatedEntityId == innerData.ApplicantId
                  && cl.IsActive);

        if (innerData.IsPrimary)
        {
            foreach (var stale in contactLinks.Where(cl => cl.IsPrimary && cl.ContactId != contactId))
            {
                stale.IsPrimary = false;
                await contactLinkRepository.UpdateAsync(stale);
            }

            var newPrimary = contactLinks.FirstOrDefault(cl => cl.ContactId == contactId && !cl.IsPrimary);
            if (newPrimary != null)
            {
                newPrimary.IsPrimary = true;
                await contactLinkRepository.UpdateAsync(newPrimary);
            }
        }
        else
        {
            var demoted = contactLinks.FirstOrDefault(cl => cl.ContactId == contactId && cl.IsPrimary);
            if (demoted != null)
            {
                demoted.IsPrimary = false;
                await contactLinkRepository.UpdateAsync(demoted);
            }
        }

        await contactRepository.UpdateAsync(contact);

        logger.LogInformation("Contact {ContactId} updated successfully", contactId);
        return "Contact updated successfully";
    }
}
