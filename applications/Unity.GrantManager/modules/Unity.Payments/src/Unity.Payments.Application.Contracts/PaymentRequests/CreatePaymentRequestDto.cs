using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Users;

namespace Unity.Payments.PaymentRequests
{
#pragma warning disable CS8618
    [Serializable]
    public class CreatePaymentRequestDto
    {
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid SiteId { get; set; }
        public string PayeeName { get; set; }
        public string ContractNumber { get; set; }
        public string SupplierNumber { get; set; }
        public string SupplierName { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public string BatchName { get; set; }
        public decimal BatchNumber { get; set; } = 0;
        public string ReferenceNumber { get;  set; } = string.Empty;
        public string SubmissionConfirmationCode { get; set; } = string.Empty;
        public string? InvoiceStatus { get;  set; }
        public string? PaymentStatus { get;  set; }
        public string? PaymentNumber { get;  set; }
        public string? PaymentDate { get; set; }
        public decimal? PaymentThreshold { get; set; } = 500000m;
    }

    public class UpdatePaymentStatusRequestDto : IValidatableObject
    {
        public Guid PaymentRequestId { get; set; }
      
        public bool IsApprove { get; set; }
        
        public Guid? PreviousApprover { get; set; }

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            var currentUser = validationContext.GetRequiredService<ICurrentUser>();
            if (PreviousApprover == currentUser.Id)
            {
                yield return new ValidationResult(
                    errorMessage: "You cannot approve this payment as you have already approved it as an L1 Approver",
                    memberNames: [nameof(PreviousApprover)]
                );
            }
        }
    }
#pragma warning restore CS8618
}

