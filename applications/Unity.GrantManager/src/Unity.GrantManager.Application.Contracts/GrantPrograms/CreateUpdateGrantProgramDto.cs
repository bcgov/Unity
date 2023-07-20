using System;
using System.ComponentModel.DataAnnotations;

using Unity.GrantManager.GrantPrograms;

public class CreateUpdateGrantProgramDto
{
    [Required]
    [StringLength(128)]
    public string ProgramName { get; set; }

    [Required]
    public GrantProgramType Type { get; set; } = GrantProgramType.Undefined;

    [Required]
    [DataType(DataType.Date)]
    public DateTime PublishDate { get; set; } = DateTime.Now;
}
