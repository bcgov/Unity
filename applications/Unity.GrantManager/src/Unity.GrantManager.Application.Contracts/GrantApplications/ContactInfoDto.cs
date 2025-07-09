using System;

namespace Unity.GrantManager.GrantApplications;

public class ContactInfoDto
{
    public Guid? ApplicantAgentId { get; set; }
    public Guid? ApplicationId { get; set; }

    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Phone2 { get; set; }
}
