namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>What one members sync changed in the tenant's Metabase group.</summary>
public class MetabaseMemberSyncResultDto
{
    public int Added { get; set; }

    /// <summary>Off-boarded members removed from the group.</summary>
    public int Removed { get; set; }

    /// <summary>Emails that should be in the group but have no Metabase account yet; added once they sign in.</summary>
    public int NotMetabaseUsers { get; set; }
}
