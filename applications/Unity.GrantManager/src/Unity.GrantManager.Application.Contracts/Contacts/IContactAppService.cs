using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.GrantManager.Contacts;

/// <summary>
/// Generic contact management service. Provides operations for creating, retrieving,
/// and managing contacts linked to any entity type via <see cref="ContactLink"/>.
/// </summary>
public interface IContactAppService
{
    /// <summary>
    /// Retrieves all active contacts linked to the specified entity.
    /// </summary>
    /// <param name="entityType">The type of the related entity (e.g. "ApplicantProfile").</param>
    /// <param name="entityId">The unique identifier of the related entity.</param>
    /// <returns>A list of <see cref="ContactDto"/> for the matching entity.</returns>
    Task<List<ContactDto>> GetContactsByEntityAsync(string entityType, Guid entityId);

    /// <summary>
    /// Creates a new contact and links it to the specified entity.
    /// If <see cref="CreateContactLinkDto.IsPrimary"/> is <c>true</c>, any existing primary
    /// contact for the same entity type and ID will be cleared first.
    /// </summary>
    /// <param name="input">The contact and link details.</param>
    /// <returns>The created <see cref="ContactDto"/>.</returns>
    Task<ContactDto> CreateContactAsync(CreateContactLinkDto input);

    /// <summary>
    /// Sets the specified contact as the primary contact for the given entity.
    /// Only one primary contact is allowed per entity type and entity ID;
    /// any existing primary will be cleared before setting the new one.
    /// </summary>
    /// <param name="entityType">The type of the related entity.</param>
    /// <param name="entityId">The unique identifier of the related entity.</param>
    /// <param name="contactId">The unique identifier of the contact to set as primary.</param>
    /// <exception cref="Volo.Abp.BusinessException">Thrown when no active contact link is found for the given parameters.</exception>
    Task SetPrimaryContactAsync(string entityType, Guid entityId, Guid contactId);
}
