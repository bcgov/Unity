using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Intakes;

[Serializable]
public class ReconcileSubmissionsInput
{
    [Required]
    public string TenantName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<ReconcileSubmissionItemDto> Submissions { get; set; } = [];
}

[Serializable]
public class ReconcileSubmissionItemDto
{
    public Guid SubmissionId { get; set; }

    public Guid FormId { get; set; }

    public Guid FormVersionId { get; set; }

    public string? ConfirmationId { get; set; }
}
