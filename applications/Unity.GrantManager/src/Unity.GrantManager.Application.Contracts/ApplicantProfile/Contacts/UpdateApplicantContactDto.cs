using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.ApplicantProfile;

/// <summary>
/// Input DTO for updating an existing applicant-scoped contact.
/// </summary>
public class UpdateApplicantContactDto
{
    /// <summary>Role/type key — see <see cref="ApplicantContactRoleOptions"/>.</summary>
    [Required]
    [MaxLength(100)]
    public string? Role { get; set; }

    /// <summary>Full name.</summary>
    [Required]
    [MinLength(2)]
    [MaxLength(250)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Job title.</summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>Email address.</summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>Mobile phone number.</summary>
    [RegularExpression(@"^[\+]?[0-9\-\.\(\)\s]*$")]
    public string? MobilePhoneNumber { get; set; }

    /// <summary>Work phone number.</summary>
    [RegularExpression(@"^[\+]?[0-9\-\.\(\)\s]*$")]
    public string? WorkPhoneNumber { get; set; }

    /// <summary>Work phone extension.</summary>
    public string? WorkPhoneExtension { get; set; }

    /// <summary>When true, other primary contact links for the applicant are demoted.</summary>
    public bool IsPrimary { get; set; }
}
