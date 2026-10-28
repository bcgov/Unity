namespace Unity.Reporting.Configuration;

/// <summary>
/// The state of one reporting view, reconciled from the Reporting schema and the
/// ReportColumnsMap mapping that generates it.
/// </summary>
public class ReportingViewStatusDto
{
    /// <summary>
    /// Gets or sets the view name.
    /// </summary>
    public string ViewName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider of the mapping that generates this view (e.g. "worksheet").
    /// Null when no mapping uses this view name.
    /// </summary>
    public string? CorrelationProvider { get; set; }

    /// <summary>
    /// Gets or sets the mapping's view generation status. Null when no mapping uses this view name.
    /// </summary>
    public ViewStatus? ViewStatus { get; set; }

    /// <summary>
    /// Gets or sets whether the view exists in the Reporting schema.
    /// </summary>
    public bool InDatabase { get; set; }

    /// <summary>
    /// Gets or sets whether the tenant's view role can SELECT from the view. Null when the view
    /// does not exist or the view role does not exist.
    /// </summary>
    public bool? Granted { get; set; }

    /// <summary>
    /// Gets or sets the most important problem with this view, if any.
    /// </summary>
    public ReportingViewIssue Issue { get; set; }
}
