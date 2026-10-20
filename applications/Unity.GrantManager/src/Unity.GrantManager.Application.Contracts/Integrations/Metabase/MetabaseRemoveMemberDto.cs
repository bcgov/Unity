using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>Sent in the request body, so the email stays out of URLs and access logs.</summary>
public class MetabaseRemoveMemberDto
{
    [Required]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
