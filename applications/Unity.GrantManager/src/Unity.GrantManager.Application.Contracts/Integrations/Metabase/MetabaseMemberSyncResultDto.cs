namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>What one members sync changed in the tenant's Metabase group.</summary>
public class MetabaseMemberSyncResultDto
{
    public int Added { get; set; }

    /// <summary>
    /// Members removed from the group: off-boarded users, and members the sync added for
    /// Metabase.Access who no longer hold it.
    /// </summary>
    public int Removed { get; set; }

    /// <summary>Emails that should be in the group but have no Metabase account yet; added once they sign in.</summary>
    public int NotMetabaseUsers { get; set; }
}
