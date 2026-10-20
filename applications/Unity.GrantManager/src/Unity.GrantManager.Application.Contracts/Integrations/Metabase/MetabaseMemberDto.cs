namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>
/// One row of the tenant's Metabase members table: an email that is in the tenant's Metabase
/// group, or that Unity wants there (a program manager or an Additional member).
/// </summary>
public class MetabaseMemberDto
{
    public string Email { get; set; } = string.Empty;

    /// <summary>The Metabase user's name; null when the email is not in the group.</summary>
    public string? Name { get; set; }

    /// <summary>Why the email is listed: <see cref="MetabaseMemberSource"/>.</summary>
    public MetabaseMemberSource Source { get; set; }

    /// <summary>Whether the email is currently a member of the tenant's Metabase group.</summary>
    public bool InGroup { get; set; }

    /// <summary>
    /// Whether the email belongs to an off-boarded Unity user in this tenant (deleted or inactive,
    /// with no active user sharing the email). Off-boarded members are removed by the next sync and
    /// never added.
    /// </summary>
    public bool Offboarded { get; set; }

    /// <summary>
    /// Whether a Metabase account exists for the email (they have signed in to Metabase). Only
    /// looked up for emails that should be added; null otherwise.
    /// </summary>
    public bool? IsMetabaseUser { get; set; }

    /// <summary>
    /// Whether Remove applies. Active program managers are managed by their role instead.
    /// </summary>
    public bool CanRemove { get; set; }
}

public enum MetabaseMemberSource
{
    /// <summary>A user with the program_manager role in this tenant.</summary>
    ProgramManager = 0,

    /// <summary>On the tenant's Additional members list (added in Unity).</summary>
    AdditionalMember = 1,

    /// <summary>In the group for no Unity reason: added directly in Metabase.</summary>
    Metabase = 2
}
