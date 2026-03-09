using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Messages;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class ContactSetPrimaryHandler(
    IContactLinkRepository contactLinkRepository,
    ILogger<ContactSetPrimaryHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "CONTACT_SET_PRIMARY_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var contactId = Guid.Parse(payload.ContactId ?? throw new ArgumentException("contactId is required"));
        var profileId = Guid.Parse(payload.ProfileId ?? throw new ArgumentException("profileId is required"));

        logger.LogInformation("Setting contact {ContactId} as primary for profile {ProfileId}", contactId, profileId);

        // Find all contact links for this profile and clear their primary flag
        var profileLinks = await contactLinkRepository.GetListAsync(
            cl => cl.RelatedEntityId == profileId && cl.IsActive);

        foreach (var link in profileLinks)
        {
            link.IsPrimary = link.ContactId == contactId;
            await contactLinkRepository.UpdateAsync(link, autoSave: true);
        }

        logger.LogInformation("Contact {ContactId} set as primary", contactId);
        return "Contact set as primary";
    }
}
