using System;

namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseTenantStatusDto
{
    /// <summary>Whether Setup can run for this tenant (enabled, Reporting on, key and URL set).</summary>
    public bool CanRun { get; set; }

    /// <summary>Why Setup can't run; null when <see cref="CanRun"/> is true.</summary>
    public string? NotRunReason { get; set; }

    /// <summary>True when Setup should run but can't (missing key or URL), false when it doesn't apply.</summary>
    public bool NotRunIsFailure { get; set; }

    /// <summary>The last Setup result (Waiting, Success, Failure, Error or Skipped); null if never recorded.</summary>
    public string? Status { get; set; }

    public string? StatusMessage { get; set; }

    public DateTime? StatusUpdatedAt { get; set; }

    public bool IsSetUp => DatabaseId.HasValue && GroupId.HasValue && CollectionId.HasValue;

    public int? DatabaseId { get; set; }

    public int? GroupId { get; set; }

    public int? CollectionId { get; set; }

    /// <summary>The Metabase site URL for links; null when the connection wasn't checked.</summary>
    public string? MetabaseUrl { get; set; }

    /// <summary>Result of the API key and URL check; null when it wasn't run (Setup can't run).</summary>
    public bool? ConnectionOk { get; set; }

    /// <summary>Why the connection check failed, or a warning (e.g. the key isn't an administrator's).</summary>
    public string? ConnectionMessage { get; set; }
}
