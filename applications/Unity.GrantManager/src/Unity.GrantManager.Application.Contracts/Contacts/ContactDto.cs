using System;

namespace Unity.GrantManager.Contacts;

/// <summary>
/// Represents a contact linked to an entity, returned by the generic contacts service.
/// </summary>
public class ContactDto
{
    /// <summary>The unique identifier of the contact.</summary>
    public Guid ContactId { get; set; }

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

    /// <summary>Whether this contact is the primary contact for the linked entity.</summary>
    public bool IsPrimary { get; set; }
}
