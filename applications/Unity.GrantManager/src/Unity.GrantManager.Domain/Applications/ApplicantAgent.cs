using System;
using System.Text.Json.Serialization;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicantAgent : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public string? OidcSubUser { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid? ApplicationId { get; set; }

    [JsonIgnore]
    public virtual Application Application
    {
        set => _application = value;
        get => _application
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Application));
    }

    private Application? _application;
    public Guid? TenantId { get; set; }

    public bool IsConfirmed { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public string RoleForApplicant { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ContactOrder { get; set; } = 0;
    public string? Phone { get; set; } = string.Empty;
    public string? Phone2 { get; set; } = string.Empty;
    public string? PhoneExtension { get; set; } = string.Empty;
    public string? Phone2Extension { get; set; } = string.Empty;
    public string? Email { get; set; } = string.Empty;
    public string? Title { get; set; } = string.Empty;

    // CHEFS - applicantAgent - Login Token
    public Guid? BceidBusinessGuid { get; set; }
    public Guid? BceidUserGuid { get; set; }
    public string? BceidUserName { get; set; } = string.Empty;
    public string? BceidBusinessName { get; set; } = string.Empty;
    public string? IdentityName { get; set; } = string.Empty;
    public string? IdentityEmail { get; set; } = string.Empty;
    public string? IdentityProvider { get; set; } = string.Empty;
}
