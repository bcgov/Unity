using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Intakes;

/// <summary>
/// Request to pull CHEFS submissions that are missing in Unity and run them through intake.
/// </summary>
[Serializable]
public class ReconcileSubmissionsInput
{
    /// <summary>
    /// Name of the tenant the submissions are created in (the Tenant filter on the Reconciliation tab).
    /// </summary>
    [Required]
    public string TenantName { get; set; } = string.Empty;

    /// <summary>
    /// The CHEFS submissions selected on the Reconciliation tab.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<ReconcileSubmissionItemDto> Submissions { get; set; } = [];
}

/// <summary>
/// One CHEFS submission to reconcile. Carries the same identifiers CHEFS sends to the
/// api/chefs/event webhook, plus the confirmation ID for reporting.
/// </summary>
[Serializable]
public class ReconcileSubmissionItemDto
{
    /// <summary>
    /// CHEFS submission ID.
    /// </summary>
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// CHEFS form ID.
    /// </summary>
    public Guid FormId { get; set; }

    /// <summary>
    /// CHEFS form version ID the submission was made against.
    /// </summary>
    public Guid FormVersionId { get; set; }

    /// <summary>
    /// CHEFS confirmation ID (the Submission # shown in the UI); becomes the Unity application reference number.
    /// </summary>
    public string? ConfirmationId { get; set; }
}
