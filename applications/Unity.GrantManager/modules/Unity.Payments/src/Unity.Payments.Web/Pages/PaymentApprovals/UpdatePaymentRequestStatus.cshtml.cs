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

        private readonly IPaymentRequestAppService _paymentRequestService;
        private readonly IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly ICurrentUser _currentUser;
        private readonly IPermissionCheckerService _permissionCheckerService;

        public UpdatePaymentRequestStatus(IPaymentRequestAppService paymentRequestService,
            IPaymentConfigurationAppService paymentConfigurationAppService,
            ICurrentUser currentUser,
            IPermissionCheckerService permissionCheckerService)
        {
            SelectedPaymentIds = [];
            _paymentRequestService = paymentRequestService;
            _paymentConfigurationAppService = paymentConfigurationAppService;
            _currentUser = currentUser;
            _permissionCheckerService = permissionCheckerService;
        }

        public async Task OnGetAsync(string paymentIds, bool isApprove)
        {
            IsApproval = isApprove;
            SelectedPaymentIds = JsonSerializer.Deserialize<List<Guid>>(paymentIds) ?? [];
            var payments = await _paymentRequestService.GetListByPaymentIdsAsync(SelectedPaymentIds);
            var permissionsToCheck = new[] { "GrantApplicationManagement.Payments.L1ApproveOrDecline", "GrantApplicationManagement.Payments.L2ApproveOrDecline", "GrantApplicationManagement.Payments.L3ApproveOrDecline" };
            var permissionResult = await _permissionCheckerService.CheckPermissionsAsync(permissionsToCheck);
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

                var verifiedRequest = CheckUserPermissions(payment.Status, permissionResult, IsApproval, payment.Amount > PaymentThreshold, request);

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

            IsErrors = paymentApprovals.Exists(p => !p.isPermitted);
        }

        private PaymentsApprovalModel CheckUserPermissions(PaymentRequestStatus status, PermissionResult permissionResult, bool IsApproval, bool isExceedThreshold, PaymentsApprovalModel request)
        {
            if (status.Equals(PaymentRequestStatus.L1Pending))
            {
                request.ToStatus = IsApproval ? PaymentRequestStatus.L2Pending : PaymentRequestStatus.L1Declined;
                request.isPermitted = _currentUser.IsInRole("l1_approver") && permissionResult.HasPermission("GrantApplicationManagement.Payments.L1ApproveOrDecline");
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

                request.isPermitted = _currentUser.IsInRole("l2_approver") && permissionResult.HasPermission("GrantApplicationManagement.Payments.L2ApproveOrDecline");
            }
            else if (status.Equals(PaymentRequestStatus.L3Pending))
            {
                request.ToStatus = IsApproval ? PaymentRequestStatus.Submitted : PaymentRequestStatus.L3Declined;
                request.isPermitted = _currentUser.IsInRole("l3_approver") && permissionResult.HasPermission("GrantApplicationManagement.Payments.L3ApproveOrDecline");
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
                        isApprove = isApprove,
                    });
                }
            }

            return payments;
        }
    }
}
