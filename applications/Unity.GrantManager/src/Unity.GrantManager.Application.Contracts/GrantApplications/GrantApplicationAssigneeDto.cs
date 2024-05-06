using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

[Serializable]
public class GrantApplicationAssigneeDto : EntityDto<Guid>
{
    public Guid AssigneeId { get; set; }
    public Guid ApplicationId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Duty { get; set; } = string.Empty;
}
