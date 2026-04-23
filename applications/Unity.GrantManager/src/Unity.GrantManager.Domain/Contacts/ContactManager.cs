using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Unity.GrantManager.Contacts;

/// <summary>
/// Domain service that owns <see cref="Contact"/> / <see cref="ContactLink"/> write invariants
/// (create, update, set-primary). Application services delegate here so they do not need to
/// inject each other.
/// </summary>
public class ContactManager(
    IContactRepository contactRepository,
    IContactLinkRepository contactLinkRepository) : DomainService, IContactManager
{
    /// <inheritdoc />
    public virtual async Task<(Contact Contact, ContactLink Link)> CreateAsync(
        string entityType,
        Guid entityId,
        ContactInput input,
        string? role,
        bool isPrimary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentNullException.ThrowIfNull(input);

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

        if (isPrimary)
        {
            await ClearPrimaryAsync(entityType, entityId);
        }

        var link = await contactLinkRepository.InsertAsync(new ContactLink
        {
            ContactId = contact.Id,
            RelatedEntityType = entityType,
            RelatedEntityId = entityId,
            Role = role,
            IsPrimary = isPrimary,
            IsActive = true
        }, autoSave: true);

        return (contact, link);
    }

    /// <inheritdoc />
    public virtual async Task<(Contact Contact, ContactLink Link)> UpdateAsync(
        string entityType,
        Guid entityId,
        Guid contactId,
        ContactInput input,
        string? role,
        bool isPrimary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentNullException.ThrowIfNull(input);

        var contact = await contactRepository.GetAsync(contactId);

        contact.Name = input.Name;
        contact.Title = input.Title;
        contact.Email = input.Email;
        contact.HomePhoneNumber = input.HomePhoneNumber;
        contact.MobilePhoneNumber = input.MobilePhoneNumber;
        contact.WorkPhoneNumber = input.WorkPhoneNumber;
        contact.WorkPhoneExtension = input.WorkPhoneExtension;

        await contactRepository.UpdateAsync(contact, autoSave: true);

        var contactLinksQuery = await contactLinkRepository.GetQueryableAsync();
        var links = await AsyncExecuter.ToListAsync(contactLinksQuery
            .Where(l => l.RelatedEntityType == entityType
                        && l.RelatedEntityId == entityId
                        && l.IsActive));

        var targetLink = links.FirstOrDefault(l => l.ContactId == contactId)
            ?? throw new BusinessException("Contacts:ContactLinkNotFound")
                .WithData("contactId", contactId)
                .WithData("entityType", entityType)
                .WithData("entityId", entityId);

        if (isPrimary)
        {
            foreach (var stale in links.Where(l => l.IsPrimary && l.ContactId != contactId))
            {
                stale.IsPrimary = false;
                await contactLinkRepository.UpdateAsync(stale, autoSave: true);
            }
        }

        var linkChanged = false;
        if (targetLink.IsPrimary != isPrimary)
        {
            targetLink.IsPrimary = isPrimary;
            linkChanged = true;
        }
        if (role is not null && targetLink.Role != role)
        {
            targetLink.Role = role;
            linkChanged = true;
        }
        if (linkChanged)
        {
            await contactLinkRepository.UpdateAsync(targetLink, autoSave: true);
        }

        return (contact, targetLink);
    }

    /// <inheritdoc />
    public virtual async Task SetPrimaryAsync(string entityType, Guid entityId, Guid contactId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);

        await ClearPrimaryAsync(entityType, entityId);

        var contactLinksQuery = await contactLinkRepository.GetQueryableAsync();
        var link = await AsyncExecuter.FirstOrDefaultAsync(contactLinksQuery
            .Where(l => l.RelatedEntityType == entityType
                        && l.RelatedEntityId == entityId
                        && l.ContactId == contactId
                        && l.IsActive))
            ?? throw new BusinessException("Contacts:ContactLinkNotFound")
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
        var currentPrimaryLinks = await AsyncExecuter.ToListAsync(contactLinksQuery
            .Where(l => l.RelatedEntityType == entityType
                        && l.RelatedEntityId == entityId
                        && l.IsPrimary
                        && l.IsActive));

        foreach (var existing in currentPrimaryLinks)
        {
            existing.IsPrimary = false;
            await contactLinkRepository.UpdateAsync(existing, autoSave: true);
        }
    }
}
