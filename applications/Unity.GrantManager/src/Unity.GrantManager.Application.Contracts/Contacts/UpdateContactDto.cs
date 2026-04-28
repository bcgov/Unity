namespace Unity.GrantManager.Contacts;

/// <summary>
/// Input DTO for updating an existing contact and (optionally) the primary/role flags
/// of its link to a related entity.
/// </summary>
public class UpdateContactDto
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

    /// <summary>Whether this contact should be flagged as the primary contact. When true, other primary links for the same entity are demoted.</summary>
    public bool IsPrimary { get; set; }
}
