using System;
using Unity.Payments.Enums;
using Unity.Payments.Suppliers;
using Volo.Abp.Application.Dtos;

namespace Unity.GrantManager.ApplicantProfile;

public class ApplicantPaymentDetailsDto : EntityDto<Guid>
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string ApplicationReferenceNo { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public string? PaymentDate { get; set; }
    public PaymentRequestStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentStatus { get; set; }
    public string? InvoiceStatus { get; set; }
    public string? CasResponse { get; set; }
    public string Category { get; set; } = string.Empty;
    public string SupplierNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public SiteDto? Site { get; set; }
}
