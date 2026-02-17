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
    IContactLinkRepository contactLinkRepository)
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
        var contact = await contactRepository.InsertAsync(new Contact
        {
            Name = input.Name,
            Title = input.Title,
            Email = input.Email,
            HomePhoneNumber = input.HomePhoneNumber,
            MobilePhoneNumber = input.MobilePhoneNumber,
            WorkPhoneNumber = input.WorkPhoneNumber,
            WorkPhoneExtension = input.WorkPhoneExtension
        }, autoSave: true);

        if (input.IsPrimary)
        {
            await ClearPrimaryAsync(input.RelatedEntityType, input.RelatedEntityId);
        }

        await contactLinkRepository.InsertAsync(new ContactLink
        {
            ContactId = contact.Id,
            RelatedEntityType = input.RelatedEntityType,
            RelatedEntityId = input.RelatedEntityId,
            Role = input.Role,
            IsPrimary = input.IsPrimary,
            IsActive = true
        }, autoSave: true);

        return new ContactDto
        {
            ContactId = contact.Id,
            Name = contact.Name,
            Title = contact.Title,
            Email = contact.Email,
            HomePhoneNumber = contact.HomePhoneNumber,
            MobilePhoneNumber = contact.MobilePhoneNumber,
            WorkPhoneNumber = contact.WorkPhoneNumber,
            WorkPhoneExtension = contact.WorkPhoneExtension,
            Role = input.Role,
            IsPrimary = input.IsPrimary
        };
    }

    /// <inheritdoc />
    public async Task SetPrimaryContactAsync(string entityType, Guid entityId, Guid contactId)
    {
        await ClearPrimaryAsync(entityType, entityId);

        var contactLinksQuery = await contactLinkRepository.GetQueryableAsync();
        var link = await contactLinksQuery
            .Where(l => l.RelatedEntityType == entityType
                        && l.RelatedEntityId == entityId
                        && l.ContactId == contactId
                        && l.IsActive)
            .FirstOrDefaultAsync() ?? throw new BusinessException("Contacts:ContactLinkNotFound")
                .WithData("contactId", contactId)
                .WithData("entityType", entityType)
                .WithData("entityId", entityId);
        link.IsPrimary = true;
        await contactLinkRepository.UpdateAsync(link, autoSave: true);
    }

    /// <summary>
    /// Clears the primary flag on all active contact links for the specified entity.
    /// </summary>
    private async Task ClearPrimaryAsync(string entityType, Guid entityId)
    {
        var contactLinksQuery = await contactLinkRepository.GetQueryableAsync();
        var currentPrimaryLinks = await contactLinksQuery
            .Where(l => l.RelatedEntityType == entityType
                        && l.RelatedEntityId == entityId
                        && l.IsPrimary
                        && l.IsActive)
            .ToListAsync();

        foreach (var existing in currentPrimaryLinks)
        {
            existing.IsPrimary = false;
            await contactLinkRepository.UpdateAsync(existing, autoSave: true);
        }
    }
}
