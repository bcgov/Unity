using System;

namespace Unity.Payments.Web.Pages.Payments
{
    public interface IPaymentFormItem
    {
        Guid CorrelationId { get; set; }
        string InvoiceNumber { get; set; }
        decimal Amount { get; set; }
        string? ParentReferenceNo { get; set; }
        string? SubmissionConfirmationCode { get; set; }
        decimal? MaximumAllowedAmount { get; set; }
        bool IsPartOfParentChildGroup { get; set; }
        decimal? ParentApprovedAmount { get; set; }
    }
}
