using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Unity.Payments.PaymentRequests;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.Payments.PaymentConfigurations;
using Unity.Payment.Shared;
using System.Text.Json;
using Unity.Payments.Enums;
using Volo.Abp.Users;
using System.Linq;
using Unity.Payments.Domain.Shared;
using Unity.Payments.Permissions;

namespace Unity.Payments.Web.Pages.PaymentApprovals
{
    public class PaymentGrouping
    {
        public int GroupId { get; set; }
        public PaymentRequestStatus ToStatus { get; set; }
        public List<PaymentsApprovalModel> Items { get; set; } = [];
    }

    public class UpdatePaymentRequestStatus : AbpPageModel
    {
        [BindProperty]
        public List<PaymentGrouping> PaymentGroupings { get; set; } = [];

        [BindProperty]
        public decimal PaymentThreshold { get; set; }

        [BindProperty]
        public bool DisableSubmit { get; set; }

        [BindProperty]
        public bool HasPaymentConfiguration { get; set; }

        [BindProperty]
        public bool IsApproval { get; set; }

        [BindProperty]
        public bool IsErrors { get; set; }

        public List<Guid> SelectedPaymentIds { get; set; }

        public string FromStatusText { get; set; } = string.Empty;

        private readonly IPaymentRequestAppService _paymentRequestService;
        private readonly IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly IPermissionCheckerService _permissionCheckerService;

        public UpdatePaymentRequestStatus(IPaymentRequestAppService paymentRequestService,
            IPaymentConfigurationAppService paymentConfigurationAppService,
            ICurrentUser currentUser,
            IPermissionCheckerService permissionCheckerService)
        {
            SelectedPaymentIds = [];
            _paymentRequestService = paymentRequestService;
            _paymentConfigurationAppService = paymentConfigurationAppService;
            _permissionCheckerService = permissionCheckerService;
        }

        public async Task OnGetAsync(string paymentIds, bool isApprove)
        {
            await GetFromStateForUserAsync();

            IsApproval = isApprove;
            SelectedPaymentIds = JsonSerializer.Deserialize<List<Guid>>(paymentIds) ?? [];
            var payments = await _paymentRequestService.GetListByPaymentIdsAsync(SelectedPaymentIds);
            var permissionsToCheck = new[] { PaymentsPermissions.Payments.L1ApproveOrDecline, PaymentsPermissions.Payments.L2ApproveOrDecline, PaymentsPermissions.Payments.L3ApproveOrDecline };
            _ = await _permissionCheckerService.CheckPermissionsAsync(permissionsToCheck);
            var paymentConfiguration = await _paymentConfigurationAppService.GetAsync();

            PaymentThreshold = paymentConfiguration?.PaymentThreshold ?? PaymentSharedConsts.DefaultThresholdAmount;
            HasPaymentConfiguration = true;

            var paymentApprovals = new List<PaymentsApprovalModel>();
            foreach (var payment in payments)
            {
                PaymentsApprovalModel request = new()
                {
                    Id = payment.Id,
                    CorrelationId = payment.Id,
                    ApplicantName = payment.PayeeName,
                    Amount = payment.Amount,
                    Description = payment.Description,
                    InvoiceNumber = payment.InvoiceNumber,
                    Status = payment.Status,
                    IsL3ApprovalRequired = payment.Amount > PaymentThreshold,
                    ToStatus = payment.Status,
                };

                var verifiedRequest = await CheckUserPermissionsAsync(payment.Status, IsApproval, payment.Amount > PaymentThreshold, request);

                if (verifiedRequest.isPermitted)
                {
                    paymentApprovals!.Add(request);
                }
            }

            var grouping = paymentApprovals.GroupBy(item => item.ToStatus)
                            .Select((g, index) => (GroupId: index, ToStatus: g.Key, Items: g.ToList()))
                            .ToList();

            var indx = 0;
            foreach (var (GroupId, ToStatus, Items) in grouping)
            {
                PaymentGroupings.Add(new PaymentGrouping()
                {
                    GroupId = GroupId,
                    Items = Items,
                    ToStatus = ToStatus
                });

                indx++;
            }

            DisableSubmit = (paymentApprovals.Count == 0);
        }

        private async Task<PaymentsApprovalModel> CheckUserPermissionsAsync(PaymentRequestStatus status, bool IsApproval, bool isExceedThreshold, PaymentsApprovalModel request)
        {
            if (status.Equals(PaymentRequestStatus.L1Pending))
            {
                request.ToStatus = IsApproval ? PaymentRequestStatus.L2Pending : PaymentRequestStatus.L1Declined;
                request.isPermitted = await _permissionCheckerService.IsGrantedAsync(PaymentsPermissions.Payments.L1ApproveOrDecline);

            }
            else if (status.Equals(PaymentRequestStatus.L2Pending))
            {
                if (isExceedThreshold)
                {
                    request.ToStatus = IsApproval ? PaymentRequestStatus.L3Pending : PaymentRequestStatus.L2Declined;
                }
                else
                {
                    request.ToStatus = IsApproval ? PaymentRequestStatus.Submitted : PaymentRequestStatus.L2Declined;
                }

                request.isPermitted = await _permissionCheckerService.IsGrantedAsync(PaymentsPermissions.Payments.L2ApproveOrDecline);
            }
            else if (status.Equals(PaymentRequestStatus.L3Pending))
            {
                request.ToStatus = IsApproval ? PaymentRequestStatus.Submitted : PaymentRequestStatus.L3Declined;
                request.isPermitted = await _permissionCheckerService.IsGrantedAsync(PaymentsPermissions.Payments.L3ApproveOrDecline);
            }
            else
            {
                request.isPermitted = false;
            }

            return request;

        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (PaymentGroupings == null || PaymentGroupings.Count == 0) return NoContent();
            var payments = MapPaymentRequests(IsApproval);

            await _paymentRequestService.UpdateStatusAsync(payments);

            return NoContent();
        }

        public async Task GetFromStateForUserAsync()
        {
            if (await _permissionCheckerService.IsGrantedAsync(PaymentsPermissions.Payments.L1ApproveOrDecline))
            {
                FromStatusText = GetStatusText(PaymentRequestStatus.L1Pending);
            }
            else if (await _permissionCheckerService.IsGrantedAsync(PaymentsPermissions.Payments.L2ApproveOrDecline))
            {
                FromStatusText = GetStatusText(PaymentRequestStatus.L2Pending);
            }
            else if (await _permissionCheckerService.IsGrantedAsync(PaymentsPermissions.Payments.L3ApproveOrDecline))
            {
                FromStatusText = GetStatusText(PaymentRequestStatus.L3Pending);
            }
        }

        private List<UpdatePaymentStatusRequestDto> MapPaymentRequests(bool isApprove)
        {
            var payments = new List<UpdatePaymentStatusRequestDto>();

            if (PaymentGroupings == null || PaymentGroupings.Count == 0) return payments;

            foreach (var grouping in PaymentGroupings)
            {
                foreach (var payment in grouping.Items)
                {
                    payments.Add(new UpdatePaymentStatusRequestDto()
                    {
                        PaymentRequestId = payment.Id,
                        IsApprove = isApprove,
                    });
                }
            }

            return payments;
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

        public static string GetStatusTextColor(PaymentRequestStatus status)
        {
            return status.ToString() switch
            {
                "L1Declined" or "L2Declined" or "L3Declined" or "PaymentFailed" => "#CE3E39",
                "Submitted" => "#5595D9",
                "Paid" => "#42814A",
                _ => "#053662",
            };
        }
    }
}
