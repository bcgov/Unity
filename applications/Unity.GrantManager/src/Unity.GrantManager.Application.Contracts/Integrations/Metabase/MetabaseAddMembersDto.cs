using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseAddMembersDto
{
    /// <summary>Emails to add to the tenant's Additional members list, separated by commas, semicolons or new lines.</summary>
    [Required]
    [StringLength(4000)]
    public string Emails { get; set; } = string.Empty;
}
