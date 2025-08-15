using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.Services;
using Unity.Payments.Domain.Shared;
using Unity.Payments.Enums;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Permissions;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Payments.Web.Pages.PaymentApprovals
{
    public class PaymentGrouping
    {
        public int GroupId { get; set; }
        public PaymentRequestStatus ToStatus { get; set; }
        public List<PaymentsApprovalModel> Items { get; set; } = new();
    }

    public class UpdatePaymentRequestStatus(
                        IPaymentsManager paymentsManager,
                        IPaymentRequestAppService paymentRequestAppService,
                        IApplicationFormAppService applicationFormAppService,
                        IPaymentRequestRepository paymentRepository,
                        IPaymentConfigurationAppService paymentConfigurationAppService,
                        IPermissionCheckerService permissionCheckerService) : AbpPageModel
    {
        [BindProperty] public List<PaymentGrouping> PaymentGroupings { get; set; } = new();
        [BindProperty] public decimal? UserPaymentThreshold { get; set; }
        [BindProperty] public decimal PaymentThreshold { get; set; }
        [BindProperty] public bool DisableSubmit { get; set; }
        [BindProperty] public bool HasPaymentConfiguration { get; set; }
        [BindProperty] public bool IsApproval { get; set; }
        [BindProperty] public bool IsErrors { get; set; }
        [BindProperty]
        public decimal TotalAmount { get; set; }

        [BindProperty]
        public string? Note { get; set; } = string.Empty;
        public List<Guid> SelectedPaymentIds { get; set; } = new();
        public string FromStatusText { get; set; } = string.Empty;

        public async Task OnGetAsync(string paymentIds, bool isApprove)
        {
            await InitializeStateAsync(paymentIds, isApprove);
            var payments = await paymentRequestAppService.GetListByPaymentIdsAsync(SelectedPaymentIds);
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

            DisableSubmit = paymentApprovals.Count == 0 || !ModelState.IsValid;
        }

        private async Task InitializeStateAsync(string paymentIds, bool isApprove)
        {
            await GetFromStateForUserAsync();
            IsApproval = isApprove;
            SelectedPaymentIds = JsonSerializer.Deserialize<List<Guid>>(paymentIds) ?? new();
            UserPaymentThreshold = await paymentRequestAppService.GetUserPaymentThresholdAsync();
            HasPaymentConfiguration = await paymentConfigurationAppService.GetAsync() != null;
        }

        private async Task<List<PaymentsApprovalModel>> BuildPaymentApprovalsAsync(List<PaymentDetailsDto> payments)
        {
            var paymentApprovals = new List<PaymentsApprovalModel>();

            foreach (var payment in payments)
            {
                var formThreshold = await applicationFormAppService.GetFormPaymentApprovalThresholdByApplicationIdAsync(payment.CorrelationId);
                if (formThreshold.HasValue && UserPaymentThreshold.HasValue)
                {
                    PaymentThreshold = formThreshold.Value < UserPaymentThreshold.Value ? formThreshold.Value : UserPaymentThreshold.Value;
                }
                else if (formThreshold.HasValue)
                {
                    PaymentThreshold = formThreshold.Value;
                }
                else if (UserPaymentThreshold.HasValue)
                {
                    PaymentThreshold = UserPaymentThreshold.Value;
                }
                else
                {
                    PaymentThreshold = 0m;
                }

                var approvalModel = await CreateApprovalModel(payment);
                ValidateApprovalModel(approvalModel);

                if (await VerifyPermissionsAsync(payment.Status, approvalModel))
                {
                    paymentApprovals.Add(approvalModel);
                }
            }

            return paymentApprovals;
        }

        private async Task<PaymentsApprovalModel> CreateApprovalModel(PaymentDetailsDto payment)
        {
            bool isL3ApprovalRequired = payment.Amount > PaymentThreshold;
            await UpdateExpenseApprovalsAsync(payment, isL3ApprovalRequired);

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
                IsL3ApprovalRequired = isL3ApprovalRequired,
                ToStatus = payment.Status,
                IsApproval = IsApproval,
                HasUserPaymentThreshold = UserPaymentThreshold.HasValue,
                PreviousL1Approver = payment.ExpenseApprovals.FirstOrDefault(x => x.Type == ExpenseApprovalType.Level1)?.DecisionUserId
            };
        }

        private async Task UpdateExpenseApprovalsAsync(PaymentDetailsDto payment, bool isL3ApprovalRequired)
        {
            if (payment.Status == PaymentRequestStatus.L2Pending)
            {
                var l3Approval = payment.ExpenseApprovals.FirstOrDefault(x => x.Type == ExpenseApprovalType.Level3);
                PaymentRequest paymentEntity = await paymentRepository.GetAsync(payment.Id);
                if (isL3ApprovalRequired && l3Approval == null)
                {
                    paymentEntity.ExpenseApprovals.Add(new ExpenseApproval(Guid.NewGuid(), ExpenseApprovalType.Level3));
                }
                else if (!isL3ApprovalRequired && l3Approval != null)
                {
                    var l3ApprovalEntity = paymentEntity.ExpenseApprovals.FirstOrDefault(x => x.Type == ExpenseApprovalType.Level3);
                    if (l3ApprovalEntity != null)
                    {
                        paymentEntity.ExpenseApprovals.Remove(l3ApprovalEntity);
                    }
                }

                await paymentRepository.UpdateAsync(paymentEntity);
            }
        }

        private void ValidateApprovalModel(PaymentsApprovalModel request)
        {
            var validationResults = new List<ValidationResult>();
            request.IsValid = Validator.TryValidateObject(request, new ValidationContext(request, LazyServiceProvider, null), validationResults, true);

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
            bool preventPayment = await paymentsManager.GetFormPreventPaymentStatusByPaymentRequestId(request.Id);
            request.ToStatus = GetNextStatus(status, request, preventPayment, IsApproval);
            var permission = GetRequiredPermission(status);
            return permission != null && await permissionCheckerService.IsGrantedAsync(permission);
        }

        private static PaymentRequestStatus GetNextStatus(PaymentRequestStatus status, PaymentsApprovalModel request, bool preventPayment, bool isApproval)
        {
            if (status == PaymentRequestStatus.L2Pending && isApproval)
            {
                if (request.IsL3ApprovalRequired)
                {
                    return PaymentRequestStatus.L3Pending;
                }
                if (preventPayment)
                {
                    return PaymentRequestStatus.FSB;
                }
                return PaymentRequestStatus.Submitted;
            }

            if (status == PaymentRequestStatus.L3Pending && isApproval)
            {
                if (preventPayment)
                {
                    return PaymentRequestStatus.FSB;
                }
                return PaymentRequestStatus.Submitted;
            }

            return status switch
            {
                PaymentRequestStatus.L1Pending => isApproval ? PaymentRequestStatus.L2Pending : PaymentRequestStatus.L1Declined,
                PaymentRequestStatus.L2Pending => PaymentRequestStatus.L2Declined,
                PaymentRequestStatus.L3Pending => PaymentRequestStatus.L3Declined,
                _ => request.ToStatus
            };
        }

        private static string? GetRequiredPermission(PaymentRequestStatus status)
        {
            return status switch
            {
                PaymentRequestStatus.L1Pending => PaymentsPermissions.Payments.L1ApproveOrDecline,
                PaymentRequestStatus.L2Pending => PaymentsPermissions.Payments.L2ApproveOrDecline,
                PaymentRequestStatus.L3Pending => PaymentsPermissions.Payments.L3ApproveOrDecline,
                _ => null
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (PaymentGroupings == null || PaymentGroupings.Count == 0 || !ModelState.IsValid) return NoContent();

            var payments = PaymentGroupings.SelectMany(group => group.Items)
            .Select(payment => new UpdatePaymentStatusRequestDto
            {
                PaymentRequestId = payment.Id,
                IsApprove = IsApproval,
                Note = Note ?? String.Empty
            })
            .ToList();

            await paymentRequestAppService.UpdateStatusAsync(payments);
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
                "FSB" => "Sent to Accounts Payable",
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
