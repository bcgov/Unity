using System;

namespace Unity.GrantManager.GrantApplications;

public class SigningAuthorityDto
{
    public Guid ApplicationId { get; set; }

    public string? SigningAuthorityFullName { get; set; }
    public string? SigningAuthorityTitle { get; set; }
    public string? SigningAuthorityEmail { get; set; }
    public string? SigningAuthorityBusinessPhone { get; set; }
    public string? SigningAuthorityCellPhone { get; set; }
}
