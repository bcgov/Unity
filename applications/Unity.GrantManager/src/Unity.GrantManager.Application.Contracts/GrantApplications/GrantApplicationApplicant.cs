using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationApplicantDto : AuditedEntityDto<Guid>
{
    public string ApplicantName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string SubSector { get; set; } = string.Empty;
}
