using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.Payment.Shared;
using Unity.Payments.Domain.PaymentThresholds;
using Unity.Payments.Domain.Shared;
using Unity.Payments.Enums;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Permissions;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Volo.Abp.Users;

namespace Unity.Payments.Web.Pages.PaymentApprovals
{
    public class PaymentGrouping
    {
        public int GroupId { get; set; }
        public PaymentRequestStatus ToStatus { get; set; }
        public List<PaymentsApprovalModel> Items { get; set; } = new();
    }

    public class UpdatePaymentRequestStatus(
                        IPaymentRequestAppService paymentRequestService,
                        IPaymentConfigurationAppService paymentConfigurationAppService,
                        IApplicationFormAppService applicationFormAppService,
                        IPaymentThresholdRepository paymentThresholdRepository,
                        ICurrentUser currentUser,
                        IPermissionCheckerService permissionCheckerService) : AbpPageModel
    {
        [BindProperty] public List<PaymentGrouping> PaymentGroupings { get; set; } = new();
        [BindProperty] public decimal PaymentThreshold { get; set; }
        [BindProperty] public bool DisableSubmit { get; set; }
        [BindProperty] public bool HasPaymentConfiguration { get; set; }
        [BindProperty] public bool IsApproval { get; set; }
        [BindProperty] public bool IsErrors { get; set; }
        public List<Guid> SelectedPaymentIds { get; set; } = new();
        public string FromStatusText { get; set; } = string.Empty;

        public async Task OnGetAsync(string paymentIds, bool isApprove)
        {
            await InitializeStateAsync(paymentIds, isApprove);
            var payments = await paymentRequestService.GetListByPaymentIdsAsync(SelectedPaymentIds);
            var paymentApprovals = await BuildPaymentApprovalsAsync(payments);

            PaymentGroupings = paymentApprovals
                .GroupBy(item => item.ToStatus)
                .Select((group, index) => new PaymentGrouping
                {
                    GroupId = index,
                    ToStatus = group.Key,
                    Items = group.ToList()
                })
                .ToList();

            DisableSubmit = !paymentApprovals.Any() || !ModelState.IsValid;
        }

        private async Task InitializeStateAsync(string paymentIds, bool isApprove)
        {
            await GetFromStateForUserAsync();
            IsApproval = isApprove;
            SelectedPaymentIds = JsonSerializer.Deserialize<List<Guid>>(paymentIds) ?? new();
            PaymentThreshold = await GetUserPaymentThresholdAsync();
            HasPaymentConfiguration = await paymentConfigurationAppService.GetAsync() != null;
        }

        private async Task<decimal> GetUserPaymentThresholdAsync()
        {
            var userThreshold = await paymentThresholdRepository.GetAsync(x => x.UserId == currentUser.Id);
            return userThreshold?.Threshold ?? PaymentSharedConsts.DefaultThresholdAmount;
        }

        private async Task<List<PaymentsApprovalModel>> BuildPaymentApprovalsAsync(List<PaymentDetailsDto> payments)
        {
            var paymentApprovals = new List<PaymentsApprovalModel>();

            foreach (var payment in payments)
            {
                var formThreshold = await applicationFormAppService.GetFormPaymentApprovalThresholdByApplicationIdAsync(payment.CorrelationId);
                PaymentThreshold = formThreshold.HasValue && formThreshold.Value < PaymentThreshold ? formThreshold.Value : PaymentThreshold;

                var request = CreateApprovalModel(payment);
                ValidateApprovalModel(request);

                if (await VerifyPermissionsAsync(payment.Status, request))
                {
                    paymentApprovals.Add(request);
                }
            }

            return paymentApprovals;
        }

        private PaymentsApprovalModel CreateApprovalModel(PaymentDetailsDto payment)
        {
            return new PaymentsApprovalModel
            {
                Id = payment.Id,
                ReferenceNumber = payment.ReferenceNumber,
                CorrelationId = payment.Id,
                ApplicantName = payment.PayeeName,
                Amount = payment.Amount,
                Description = payment.Description,
                InvoiceNumber = payment.InvoiceNumber,
                Status = payment.Status,
                IsL3ApprovalRequired = payment.Amount > PaymentThreshold,
                ToStatus = payment.Status,
                IsApproval = IsApproval,
                PreviousL1Approver = payment.ExpenseApprovals.FirstOrDefault(x => x.Type == ExpenseApprovalType.Level1)?.DecisionUserId
            };
        }

        private void ValidateApprovalModel(PaymentsApprovalModel request)
        {
            var validationContext = new ValidationContext(request, LazyServiceProvider, null);
            var validationResults = new List<ValidationResult>();
            request.IsValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

            foreach (var validationResult in validationResults)
            {
                foreach (var memberName in validationResult.MemberNames)
                {
                    ModelState.AddModelError(memberName, validationResult.ErrorMessage ?? "Validation error.");
                }
            }
        }

        private async Task<bool> VerifyPermissionsAsync(PaymentRequestStatus status, PaymentsApprovalModel request)
        {
            request.ToStatus = status switch
            {
                PaymentRequestStatus.L1Pending => IsApproval ? PaymentRequestStatus.L2Pending : PaymentRequestStatus.L1Declined,
                PaymentRequestStatus.L2Pending => IsApproval
                    ? (request.IsL3ApprovalRequired ? PaymentRequestStatus.L3Pending : PaymentRequestStatus.Submitted)
                    : PaymentRequestStatus.L2Declined,
                PaymentRequestStatus.L3Pending => IsApproval ? PaymentRequestStatus.Submitted : PaymentRequestStatus.L3Declined,
                _ => request.ToStatus
            };

            var permission = status switch
            {
                PaymentRequestStatus.L1Pending => PaymentsPermissions.Payments.L1ApproveOrDecline,
                PaymentRequestStatus.L2Pending => PaymentsPermissions.Payments.L2ApproveOrDecline,
                PaymentRequestStatus.L3Pending => PaymentsPermissions.Payments.L3ApproveOrDecline,
                _ => null
            };

            return permission != null && await permissionCheckerService.IsGrantedAsync(permission);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (PaymentGroupings == null || !PaymentGroupings.Any() || !ModelState.IsValid) return NoContent();

            var payments = PaymentGroupings.SelectMany(group => group.Items)
                .Select(payment => new UpdatePaymentStatusRequestDto
                {
                    PaymentRequestId = payment.Id,
                    IsApprove = IsApproval
                })
                .ToList();

            await paymentRequestService.UpdateStatusAsync(payments);
            return NoContent();
        }

        private async Task GetFromStateForUserAsync()
        {
            var permissions = new[]
            {
                PaymentsPermissions.Payments.L1ApproveOrDecline,
                PaymentsPermissions.Payments.L2ApproveOrDecline,
                PaymentsPermissions.Payments.L3ApproveOrDecline
            };

            foreach (var permission in permissions)
            {
                if (await permissionCheckerService.IsGrantedAsync(permission))
                {
                    FromStatusText = GetStatusText((PaymentRequestStatus)Array.IndexOf(permissions, permission));
                    break;
                }
            }
        }

        public static string GetStatusText(PaymentRequestStatus status)
        {
            return status.ToString() switch
            {
                "L1Pending" => "L1 Pending",
                "L1Approved" => "L1 Approved",
                "L1Declined" => "L1 Declined",
                "L2Pending" => "L2 Pending",
                "L2Approved" => "L2 Approved",
                "L2Declined" => "L2 Declined",
                "L3Pending" => "L3 Pending",
                "L3Approved" => "L3 Approved",
                "L3Declined" => "L3 Declined",
                "Submitted" => "Submitted to CAS",
                "Paid" => "Paid",
                "PaymentFailed" => "Payment Failed",
                _ => "L1 Pending",
            };
        }

        public static string GetStatusTextColor(PaymentRequestStatus status) => status switch
        {
            PaymentRequestStatus.L1Declined or PaymentRequestStatus.L2Declined or PaymentRequestStatus.L3Declined => "#CE3E39",
            PaymentRequestStatus.Submitted => "#5595D9",
            PaymentRequestStatus.Paid => "#42814A",
            _ => "#053662"
        };
    }
}
