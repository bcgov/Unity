using System;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.GrantApplications;

public class GrantApplicationLiteDto : AuditedEntityDto<Guid>
{
    public string ProjectName { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    
}
