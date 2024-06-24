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
using static System.Runtime.InteropServices.JavaScript.JSType;


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
         
        public string FromStatusText {  get; set; } = string.Empty;

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
            GetFromStateForUser();
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

            DisableSubmit = (paymentApprovals.Count == 0);
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

        public void  GetFromStateForUser()
        {
            if (_currentUser.IsInRole("l1_approver"))
            {
                FromStatusText = GetStatusText(PaymentRequestStatus.L1Pending);
            }
            else if (_currentUser.IsInRole("l2_approver"))
            {
                FromStatusText = GetStatusText(PaymentRequestStatus.L2Pending);
            }
            else if(_currentUser.IsInRole("l3_approver"))
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
                        isApprove = isApprove,
                    });
                }
            }

            return payments;
        }
        public static string GetStatusText(PaymentRequestStatus status)
        {
            switch (status.ToString())
            {

                case "L1Pending":
                    return "L1 Pending";

                case "L1Approved":
                    return "L1 Approved";

                case "L1Declined":
                    return "L1 Declined";

                case "L2Pending":
                    return "L2 Pending";

                case "L2Approved":
                    return "L2 Approved";

                case "L2Declined":
                    return "L2 Declined";

                case "L3Pending":
                    return "L3 Pending";

                case "L3Approved":
                    return "L3 Approved";

                case "L3Declined":
                    return "L3 Declined";

                case "Submitted":
                    return "Submitted to CAS";

                case "Paid":
                    return "Paid";

                case "PaymentFailed":
                    return "Payment Failed";


                default:
                    return "L1 Pending";
            }
        }

        public static string GetStatusTextColor(PaymentRequestStatus status)
        {
            switch (status.ToString())
            {

                case "L1Pending":
                    return "#053662";

                case "L1Declined":
                    return "#CE3E39";

                case "L2Pending":
                    return "#053662";

                case "L2Declined":
                    return "#CE3E39";

                case "L3Pending":
                    return "#053662";

                case "L3Declined":
                    return "#CE3E39";

                case "Submitted":
                    return "#5595D9";

                case "Paid":
                    return "#42814A";

                case "PaymentFailed":
                    return "#CE3E39";

                default:
                    return "#053662";
            }
        }
    }
}
