using System;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationAssigneeDto
{
    public Guid Id { get; set; }   
    public Guid AssigneeId { get; set; }
    public string FullName { get; set; } = string.Empty;
}
