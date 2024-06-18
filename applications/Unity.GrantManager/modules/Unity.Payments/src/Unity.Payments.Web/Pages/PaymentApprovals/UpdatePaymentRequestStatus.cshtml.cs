using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Unity.Payments.Suppliers;
using Unity.Payments.PaymentRequests;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.Payments.PaymentConfigurations;
using Unity.GrantManager.GrantApplications;
using Microsoft.AspNetCore.Mvc.Rendering;
using Unity.Payment.Shared;
using System.Text.Json;
using Unity.Payments.Enums;
using Volo.Abp.Users;
using System.Linq;
using Unity.Payments.Domain.Shared;
using Volo.Abp.AspNetCore.Mvc.UI.Theming;
using System.Net.NetworkInformation;


namespace Unity.Payments.Web.Pages.PaymentApprovals
{
    public class UpdatePaymentRequestStatus : AbpPageModel
    {
        [BindProperty]
        public List<PaymentsApprovalModel> ApplicationPaymentApprovalForm { get; set; } = [];
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
        [BindProperty]

        public List<(int GroupId, PaymentRequestStatus ToStatus, List<PaymentsApprovalModel> Items)> GroupedItems { get; set; }


        private readonly IGrantApplicationAppService _applicationService;
        private readonly IPaymentRequestAppService _paymentRequestService;
        private readonly IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly ISupplierAppService _iSupplierAppService;
        private readonly ICurrentUser _currentUser;
        private readonly IPermissionCheckerService _permissionCheckerService;
      
        public UpdatePaymentRequestStatus(IGrantApplicationAppService applicationService,
           ISupplierAppService iSupplierAppService,
           IPaymentRequestAppService paymentRequestService,
           IPaymentConfigurationAppService paymentConfigurationAppService,
            ICurrentUser currentUser, IPermissionCheckerService permissionCheckerService)
        {
            SelectedPaymentIds = [];
            _applicationService = applicationService;
            _paymentRequestService = paymentRequestService;
            _paymentConfigurationAppService = paymentConfigurationAppService;
            _iSupplierAppService = iSupplierAppService;
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

                    

                    ApplicationPaymentApprovalForm!.Add(request);
                }
              
            }
            GroupedItems = ApplicationPaymentApprovalForm.GroupBy(item => item.ToStatus)
                            .Select((g, index) => (GroupId : index, ToStatus: g.Key, Items: g.ToList()))
                            .ToList();

            IsErrors = ApplicationPaymentApprovalForm.Exists(p => !p.isPermitted);

        }



        private PaymentsApprovalModel CheckUserPermissions(PaymentRequestStatus status, PermissionResult permissionResult, bool IsApproval, bool isExceedThreshold, PaymentsApprovalModel request)
        { 
      
        
            if(status.Equals(PaymentRequestStatus.L1Pending))
            {
                    request.ToStatus = IsApproval ? PaymentRequestStatus.L2Pending :  PaymentRequestStatus.L1Declined;
                    request.isPermitted = _currentUser.IsInRole("l1_approver") && permissionResult.HasPermission("GrantApplicationManagement.Payments.L1ApproveOrDecline");
            } 
            else if(status.Equals(PaymentRequestStatus.L2Pending))
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
            else if(status.Equals(PaymentRequestStatus.L3Pending))
            {
                    request.ToStatus = IsApproval ? PaymentRequestStatus.Submitted :PaymentRequestStatus.L3Declined;
                    request.isPermitted = _currentUser.IsInRole("l3_approver") && permissionResult.HasPermission("GrantApplicationManagement.Payments.L3ApproveOrDecline");
            }
            else{
                    request.isPermitted = false;
            }
            
           return request;

        }

        public async Task<IActionResult> OnPostAsync()
        {

            if (ApplicationPaymentApprovalForm == null) return NoContent();
            var payments = MapPaymentRequests(IsApproval);

            await _paymentRequestService.UpdateStatusAsync(payments);

            return NoContent();
        }

        private List<UpdatePaymentStatusRequestDto> MapPaymentRequests(bool isApprove)
        {
            var payments = new List<UpdatePaymentStatusRequestDto>();

            if (ApplicationPaymentApprovalForm == null) return payments;

            foreach (var payment in ApplicationPaymentApprovalForm)
            {
                payments.Add(new UpdatePaymentStatusRequestDto()
                {
                    PaymentRequestId = payment.Id,
                    isApprove = isApprove,

                }) ;
            }

            return payments;
        }
    }
}
