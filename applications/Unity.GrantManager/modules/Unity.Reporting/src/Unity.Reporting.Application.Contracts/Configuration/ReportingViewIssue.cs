namespace Unity.Reporting.Configuration;

/// <summary>
/// The most important problem found with a reporting view, in priority order.
/// </summary>
public enum ReportingViewIssue
{
    /// <summary>The view exists, generated successfully, and the view role can read it.</summary>
    None = 0,

    /// <summary>The mapping's last view generation failed.</summary>
    GenerationFailed = 1,

    /// <summary>The mapping is still generating (or the generation job never finished).</summary>
    Generating = 2,

    /// <summary>The mapping generated successfully but the view is not in the Reporting schema.</summary>
    Missing = 3,

    /// <summary>The view is in the Reporting schema but no mapping uses its name (e.g. left behind by a rename).</summary>
    Unmapped = 4,

    /// <summary>The view role cannot SELECT from the view.</summary>
    NotGranted = 5
}
