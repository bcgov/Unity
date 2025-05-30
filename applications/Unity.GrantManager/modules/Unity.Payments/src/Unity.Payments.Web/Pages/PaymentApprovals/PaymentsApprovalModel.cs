using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Unity.Payments.Enums;
using Unity.Payments.Localization;
using Volo.Abp.Users;

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

        public bool IsL3ApprovalRequired { get; set; }

        public PaymentRequestStatus ToStatus { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string ToStatusText { get; set; } = string.Empty;

        public Guid? PreviousL1Approver { get; set; }

        public bool IsApproval { get; set; }
        public bool IsValid { get; set; } = false;
        public Guid CurrentUser { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var currentUser = validationContext.GetRequiredService<ICurrentUser>();
            var localizer = validationContext.GetRequiredService<IStringLocalizer<PaymentsResource>>();

            // Rule AB#26693: Reject Payment Request update batch if violates L1 and L2 separation of duties
            if (IsApproval
                && Status == PaymentRequestStatus.L2Pending
                && PreviousL1Approver == currentUser.Id)
            {
                yield return new ValidationResult(
                    errorMessage: localizer["ApplicationPaymentRequest:Validations:L2ApproverRestriction"],
                    memberNames: [nameof(PreviousL1Approver)]
                );
            }
        }
    }
}
