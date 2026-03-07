using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class ContactDeleteHandler(
    IContactLinkRepository contactLinkRepository,
    ILogger<ContactDeleteHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "CONTACT_DELETE_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var contactId = Guid.Parse(payload.ContactId ?? throw new ArgumentException("contactId is required"));

        logger.LogInformation("Deleting (deactivating) contact {ContactId} for profile {ProfileId}", contactId, payload.ProfileId);

        // Soft-delete by deactivating contact links
        var links = await contactLinkRepository.GetListAsync(cl => cl.ContactId == contactId && cl.IsActive);

        foreach (var link in links)
        {
            link.IsActive = false;
            await contactLinkRepository.UpdateAsync(link, autoSave: true);
        }

        logger.LogInformation("Contact {ContactId} deactivated successfully", contactId);
        return "Contact deleted successfully";
    }
}
