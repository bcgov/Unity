using System;
using Unity.Payments.Enums;
using Volo.Abp.Application.Dtos;

namespace Unity.Payments.Suppliers;

[Serializable]
public class SiteDto : AuditedEntityDto<Guid>
{
    public string Number { get; set; } = null!;
    public PaymentGroup PaymentGroup { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public Guid SupplierId { get; set; }
}
