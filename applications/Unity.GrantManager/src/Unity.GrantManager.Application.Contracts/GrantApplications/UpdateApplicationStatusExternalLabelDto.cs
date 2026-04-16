using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.GrantApplications;

public class UpdateApplicationStatusExternalLabelDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(ApplicationStatusConsts.MaxNameLength, MinimumLength = 1)]
    public string ExternalStatus { get; set; } = string.Empty;
}
