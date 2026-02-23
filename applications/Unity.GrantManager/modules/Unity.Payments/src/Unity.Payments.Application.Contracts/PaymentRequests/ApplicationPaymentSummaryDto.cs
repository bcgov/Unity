using System;

namespace Unity.Payments.PaymentRequests;

[Serializable]
public class ApplicationPaymentSummaryDto
{
    public Guid ApplicationId { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalPending { get; set; }
}
