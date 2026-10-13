using System;

namespace Unity.GrantManager.Intakes;

/// <summary>
/// Outcome of reconciling a single CHEFS submission.
/// </summary>
[Serializable]
public class ReconcileSubmissionResultDto
{
    /// <summary>
    /// CHEFS submission ID.
    /// </summary>
    public Guid SubmissionId { get; set; }

    /// <summary>
    /// CHEFS confirmation ID (Submission #), echoed back for the success/failure message.
    /// </summary>
    public string? ConfirmationId { get; set; }

    /// <summary>
    /// True when an application was created in Unity for the submission.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Reason the submission was not reconciled; null on success.
    /// </summary>
    public string? Message { get; set; }
}
