using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.GrantApplications;

public class UpdateApplicationStatusExternalLabelsDto
{
    [Required]
    public List<UpdateApplicationStatusExternalLabelDto> Statuses { get; set; } = [];
}
