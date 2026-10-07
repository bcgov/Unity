using System;

namespace Unity.GrantManager.Intakes;

[Serializable]
public class ReconcileSubmissionResultDto
{
    public Guid SubmissionId { get; set; }

    public string? ConfirmationId { get; set; }

    public bool Success { get; set; }

    public string? Message { get; set; }
}
