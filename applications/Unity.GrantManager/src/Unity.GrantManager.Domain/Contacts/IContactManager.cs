using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.Contacts;

/// <summary>
/// Domain service for managing <see cref="Contact"/> and <see cref="ContactLink"/> writes.
/// Centralises primary-contact invariants so application services can stay thin and do not
/// need to call each other across module boundaries.
/// </summary>
public interface IContactManager
{
    /// <summary>
    /// Creates a new contact and links it to the specified related entity.
    /// When <paramref name="isPrimary"/> is true, any existing primary link on the entity is cleared first.
    /// </summary>
    Task<(Contact Contact, ContactLink Link)> CreateAsync(
        string entityType,
        Guid entityId,
        ContactInput input,
        string? role,
        bool isPrimary);

    /// <summary>
    /// Updates an existing contact's fields and its link to the specified related entity.
    /// When <paramref name="isPrimary"/> is true, any other primary link on the entity is cleared first.
    /// <paramref name="role"/> is only applied when non-null, matching legacy behaviour.
    /// </summary>
    Task<(Contact Contact, ContactLink Link)> UpdateAsync(
        string entityType,
        Guid entityId,
        Guid contactId,
        ContactInput input,
        string? role,
        bool isPrimary);

    /// <summary>
    /// Marks the specified contact as the primary contact for the given related entity,
    /// clearing the primary flag on any other active links.
    /// </summary>
    Task SetPrimaryAsync(string entityType, Guid entityId, Guid contactId);
}
