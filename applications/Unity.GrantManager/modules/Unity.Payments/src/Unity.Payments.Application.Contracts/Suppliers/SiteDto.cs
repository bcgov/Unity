using System;
using Unity.Payments.Enums;

namespace Unity.Payments.Suppliers;

[Serializable]
public class SiteDto
{
    public Guid Id { get; set; }
    public string Number { get; set; } = null!;
    public PaymentGroupDto PaymentGroup { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? AddressLine3 { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public Guid SupplierId { get; set; }
}
