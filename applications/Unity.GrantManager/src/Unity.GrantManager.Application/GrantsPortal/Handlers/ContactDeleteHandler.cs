using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class ContactDeleteHandler(
    IContactRepository contactRepository,
    IContactLinkRepository contactLinkRepository,
    ILogger<ContactDeleteHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "CONTACT_DELETE_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var contactId = Guid.Parse(payload.ContactId ?? throw new ArgumentException("contactId is required"));

        logger.LogInformation("Deleting contact {ContactId} for profile {ProfileId}", contactId, payload.ProfileId);

        // Delete all contact links first (FK dependency)
        var links = await contactLinkRepository.GetListAsync(cl => cl.ContactId == contactId);
        if (links.Count > 0)
        {
            await contactLinkRepository.DeleteManyAsync(links);
        }

        // Delete the contact
        var contact = await contactRepository.FindAsync(contactId);
        if (contact != null)
        {
            await contactRepository.DeleteAsync(contact);
        }

        logger.LogInformation("Contact {ContactId} deleted successfully", contactId);
        return "Contact deleted successfully";
    }
}
