namespace Unity.GrantManager.Contacts;

/// <summary>
/// Domain-layer input for creating or updating a <see cref="Contact"/>.
/// Kept free of application-layer DTO dependencies so Domain remains self-contained.
/// </summary>
public record ContactInput(
    string Name,
    string? Title,
    string? Email,
    string? HomePhoneNumber,
    string? MobilePhoneNumber,
    string? WorkPhoneNumber,
    string? WorkPhoneExtension);
