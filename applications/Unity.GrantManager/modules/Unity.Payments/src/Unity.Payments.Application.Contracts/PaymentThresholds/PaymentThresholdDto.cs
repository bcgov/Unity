using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.PaymentThresholds;

[Serializable]
public class PaymentThresholdDto : AuditedEntityDto<Guid>
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }        
    public string? UserName { get; set; }
    public decimal? Threshold { get; set; }
    public string? Description { get; set; }
}
