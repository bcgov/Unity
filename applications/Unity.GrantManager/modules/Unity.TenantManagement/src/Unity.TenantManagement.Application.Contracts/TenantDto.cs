using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace Unity.TenantManagement;

public class TenantDto : ExtensibleEntityDto<Guid>, IHasConcurrencyStamp
{
    public string Name { get; set; }
    public string Division { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CasClientCode { get; set; } = string.Empty;
    public string ConcurrencyStamp { get; set; }
}
