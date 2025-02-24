using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using Unity.Payments.Enums;
using Volo.Abp.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;

namespace Unity.Payments.Web.Pages.PaymentApprovals
{
    public class PaymentsApprovalModel : IValidatableObject
    {
        [Required]
        public Guid Id { get; set; }

        [DisplayName("ApplicationPaymentStatusRequest:Id")]
        [Required]
        public string ReferenceNumber { get; set; } = string.Empty;

        [DisplayName("ApplicationPaymentStatusRequest:Amount")]
        [Required]
        public decimal Amount { get; set; }
        [DisplayName("ApplicationPaymentStatusRequest:Description")]
        public string? Description { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentStatusRequest:InvoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid CorrelationId { get; set; }
        [Required(ErrorMessage = "This field is required.")]
        [DisplayName("ApplicationPaymentStatusRequest:SiteNumber")]
        public string? ApplicantName { get; set; }

        public PaymentRequestStatus Status { get; set; }

        public bool isPermitted { get; set; }

        public List<string> ErrorList { get; set; } = new List<string> { };

        public bool IsL3ApprovalRequired { get; set; }
        public Guid? PreviousApprover { get; set; }
        public bool IsSameApprover { get; set; }

        public PaymentRequestStatus ToStatus { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string ToStatusText { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            var currentUser = validationContext.GetRequiredService<ICurrentUser>();
            if (IsSameApprover || PreviousApprover == currentUser.Id)
            {
                yield return new ValidationResult(
                    "You cannot approve this payment as you have already approved it as an L1 Approver",
                    new[] { "Name", "Description" }
                );
            }
        }

    }
}
