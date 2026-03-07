using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Contacts;
using Unity.GrantManager.GrantsPortal.Messages;
using Unity.GrantManager.GrantsPortal.Messages.Commands;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantsPortal.Handlers;

public class ContactEditHandler(
    IContactRepository contactRepository,
    ILogger<ContactEditHandler> logger) : IPortalCommandHandler, ITransientDependency
{
    public string DataType => "CONTACT_EDIT_COMMAND";

    [UnitOfWork]
    public virtual async Task<string> HandleAsync(PluginDataPayload payload)
    {
        var contactId = Guid.Parse(payload.ContactId ?? throw new ArgumentException("contactId is required"));
        var innerData = payload.Data?.ToObject<ContactEditData>()
                        ?? throw new ArgumentException("Contact data is required");

        logger.LogInformation("Editing contact {ContactId} for profile {ProfileId}", contactId, payload.ProfileId);

        var contact = await contactRepository.GetAsync(contactId);

        contact.Name = innerData.Name;
        contact.Email = innerData.Email;
        contact.Title = innerData.Title;
        contact.HomePhoneNumber = innerData.HomePhoneNumber;
        contact.MobilePhoneNumber = innerData.MobilePhoneNumber;
        contact.WorkPhoneNumber = innerData.WorkPhoneNumber;
        contact.WorkPhoneExtension = innerData.WorkPhoneExtension;

        await contactRepository.UpdateAsync(contact, autoSave: true);

        logger.LogInformation("Contact {ContactId} updated successfully", contactId);
        return "Contact updated successfully";
    }
}
