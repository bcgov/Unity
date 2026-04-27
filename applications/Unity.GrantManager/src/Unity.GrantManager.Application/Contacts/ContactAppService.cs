using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Contacts;

/// <summary>
/// Generic contact management service. Manages contacts and their links to arbitrary entity types.
/// Currently marked as <c>[RemoteService(false)]</c> — not exposed as an HTTP endpoint.
/// Authorization roles to be configured before enabling remote access.
/// </summary>

[Authorize]
[RemoteService(false)]
[ExposeServices(typeof(ContactAppService), typeof(IContactAppService))]
public class ContactAppService(
    IContactRepository contactRepository,
    IContactLinkRepository contactLinkRepository,
    IContactManager contactManager)
    : GrantManagerAppService, IContactAppService
{
    /// <inheritdoc />
    public async Task<List<ContactDto>> GetContactsByEntityAsync(string entityType, Guid entityId)
    {
        var contactLinksQuery = await contactLinkRepository.GetQueryableAsync();
        var contactsQuery = await contactRepository.GetQueryableAsync();

        return await (
            from link in contactLinksQuery
            join contact in contactsQuery on link.ContactId equals contact.Id
            where link.RelatedEntityType == entityType
                  && link.RelatedEntityId == entityId
                  && link.IsActive
            select new ContactDto
            {
                ContactId = contact.Id,
                Name = contact.Name,
                Title = contact.Title,
                Email = contact.Email,
                HomePhoneNumber = contact.HomePhoneNumber,
                MobilePhoneNumber = contact.MobilePhoneNumber,
                WorkPhoneNumber = contact.WorkPhoneNumber,
                WorkPhoneExtension = contact.WorkPhoneExtension,
                Role = link.Role,
                IsPrimary = link.IsPrimary
            }).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<ContactDto> CreateContactAsync(CreateContactLinkDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var (contact, link) = await contactManager.CreateAsync(
            input.RelatedEntityType,
            input.RelatedEntityId,
            ToContactInput(input),
            input.Role,
            input.IsPrimary);

        return MapToDto(contact, link);
    }

    /// <inheritdoc />
    public async Task<ContactDto> UpdateContactAsync(string entityType, Guid entityId, Guid contactId, UpdateContactDto input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var (contact, link) = await contactManager.UpdateAsync(
            entityType,
            entityId,
            contactId,
            ToContactInput(input),
            input.Role,
            input.IsPrimary);

        return MapToDto(contact, link);
    }

    /// <inheritdoc />
    public Task SetPrimaryContactAsync(string entityType, Guid entityId, Guid contactId)
    {
        return contactManager.SetPrimaryAsync(entityType, entityId, contactId);
    }

    private static ContactInput ToContactInput(CreateContactLinkDto input) =>
        new(input.Name,
            input.Title,
            input.Email,
            input.HomePhoneNumber,
            input.MobilePhoneNumber,
            input.WorkPhoneNumber,
            input.WorkPhoneExtension);

    private static ContactInput ToContactInput(UpdateContactDto input) =>
        new(input.Name,
            input.Title,
            input.Email,
            input.HomePhoneNumber,
            input.MobilePhoneNumber,
            input.WorkPhoneNumber,
            input.WorkPhoneExtension);

    private static ContactDto MapToDto(Contact contact, ContactLink link) =>
        new()
        {
            ContactId = contact.Id,
            Name = contact.Name,
            Title = contact.Title,
            Email = contact.Email,
            HomePhoneNumber = contact.HomePhoneNumber,
            MobilePhoneNumber = contact.MobilePhoneNumber,
            WorkPhoneNumber = contact.WorkPhoneNumber,
            WorkPhoneExtension = contact.WorkPhoneExtension,
            Role = link.Role,
            IsPrimary = link.IsPrimary
        };
}
