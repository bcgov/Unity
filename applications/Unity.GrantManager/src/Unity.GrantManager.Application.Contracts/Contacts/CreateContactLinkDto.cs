using System;

namespace Unity.GrantManager.Contacts;

/// <summary>
/// Input DTO for creating a new contact and linking it to a related entity.
/// </summary>
public class CreateContactLinkDto
{
    /// <summary>The full name of the contact.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The job title of the contact.</summary>
    public string? Title { get; set; }

    /// <summary>The email address of the contact.</summary>
    public string? Email { get; set; }

    /// <summary>The home phone number of the contact.</summary>
    public string? HomePhoneNumber { get; set; }

    /// <summary>The mobile phone number of the contact.</summary>
    public string? MobilePhoneNumber { get; set; }

    /// <summary>The work phone number of the contact.</summary>
    public string? WorkPhoneNumber { get; set; }

    /// <summary>The work phone extension of the contact.</summary>
    public string? WorkPhoneExtension { get; set; }

    /// <summary>The role of the contact within the linked entity context.</summary>
    public string? Role { get; set; }

    /// <summary>Whether this contact should be set as the primary contact. Only one primary is allowed per entity type and entity ID.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>The type of the entity to link the contact to (e.g. "ApplicantProfile").</summary>
    public string RelatedEntityType { get; set; } = string.Empty;

    /// <summary>The unique identifier of the related entity.</summary>
    public Guid RelatedEntityId { get; set; }
}
