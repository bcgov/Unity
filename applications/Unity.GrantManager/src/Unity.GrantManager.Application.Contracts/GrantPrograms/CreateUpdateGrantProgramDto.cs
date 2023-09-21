using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.GrantPrograms;

public class CreateUpdateGrantProgramDto
{
    [Required]
    [StringLength(128)]
    public string ProgramName { get; set; } = string.Empty;

    [Required]
    public GrantProgramType Type { get; set; } = GrantProgramType.Undefined;

    [Required]
    [DataType(DataType.Date)]
    public DateTime PublishDate { get; set; } = DateTime.Now;
}
