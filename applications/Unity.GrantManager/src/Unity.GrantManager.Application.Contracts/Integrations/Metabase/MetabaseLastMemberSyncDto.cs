using System;

namespace Unity.GrantManager.Integrations.Metabase;

/// <summary>When the tenant's members were last synced, and what changed or why it failed.</summary>
public class MetabaseLastMemberSyncDto
{
    public DateTime SyncedAt { get; set; }

    public int Added { get; set; }

    public int Removed { get; set; }

    public int NotMetabaseUsers { get; set; }

    /// <summary>Why the sync failed; null when it succeeded.</summary>
    public string? Error { get; set; }
}
