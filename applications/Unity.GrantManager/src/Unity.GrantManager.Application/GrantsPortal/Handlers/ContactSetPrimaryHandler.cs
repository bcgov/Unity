using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.GrantsPortal.Messages.Commands;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class ContactSetPrimaryHandler(
    IContactLinkRepository contactLinkRepository,
    ILogger<ContactSetPrimaryHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    private const string ApplicantEntityType = "Applicant";

    public string DataType => "CONTACT_SET_PRIMARY_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var contactId = Guid.Parse(payload.ContactId ?? throw new ArgumentException("contactId is required"));
        var profileId = Guid.Parse(payload.ProfileId ?? throw new ArgumentException("profileId is required"));
        var innerData = payload.Data?.ToObject<ContactSetPrimaryData>()
                        ?? throw new ArgumentException("Contact data is required");

        if (innerData.ApplicantId == Guid.Empty)
        {
            throw new ArgumentException("applicantId is required");
        }

        logger.LogInformation("Setting contact {ContactId} as primary for profile {ProfileId}", contactId, profileId);

        // Only update links whose primary flag actually needs to change
        var contactLinks = await contactLinkRepository.GetListAsync(
            cl => cl.RelatedEntityType == ApplicantEntityType
                  && cl.RelatedEntityId == innerData.ApplicantId
                  && cl.IsActive);

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

        logger.LogInformation("Contact {ContactId} set as primary", contactId);
        return "Contact set as primary";
    }
}
