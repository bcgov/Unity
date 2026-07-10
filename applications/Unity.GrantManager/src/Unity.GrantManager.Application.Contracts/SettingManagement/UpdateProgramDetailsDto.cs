using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.SettingManagement;

public class UpdateProgramDetailsDto
{
    [StringLength(ProgramDetailsConsts.MaxDisplayNameLength)]
    public string? DisplayName { get; set; }

    [StringLength(ProgramDetailsConsts.MaxDivisionLength)]
    public string? Division { get; set; }

    [StringLength(ProgramDetailsConsts.MaxBranchLength)]
    public string? Branch { get; set; }

    [StringLength(ProgramDetailsConsts.MaxDescriptionLength)]
    public string? Description { get; set; }
}
