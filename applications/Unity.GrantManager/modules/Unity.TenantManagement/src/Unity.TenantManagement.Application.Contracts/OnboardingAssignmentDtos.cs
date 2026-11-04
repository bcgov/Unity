#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.TenantManagement;

public class OnboardingAssigneeDto
{
    public Guid AssigneeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class OnboardingAssignableUserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class OnboardingAssigneeInputDto
{
    public Guid AssigneeId { get; set; }

    [StringLength(OnboardingAssignmentInputDto.MaxRoleLength)]
    public string? Role { get; set; }
}

public class OnboardingAssignmentInputDto
{
    public const int MaxRoleLength = 250;

    [Required]
    [MinLength(1)]
    public List<Guid> ApplicationIds { get; set; } = [];

    public Guid? OwnerId { get; set; }

    public List<OnboardingAssigneeInputDto> Assignees { get; set; } = [];
}
