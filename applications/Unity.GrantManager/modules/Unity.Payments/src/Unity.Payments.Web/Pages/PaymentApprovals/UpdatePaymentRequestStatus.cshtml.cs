using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Unity.Payments.PaymentRequests;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using System.Text.Json;
using Unity.Payments.Enums;


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

        public PaymentRequestStatus Status { get; set; }
        public List<Guid> SelectedPaymentIds { get; set; }
        private readonly IPaymentRequestAppService _paymentRequestService;

        public UpdatePaymentRequestStatus(IPaymentRequestAppService paymentRequestService)
        {
            SelectedPaymentIds = [];
            _paymentRequestService = paymentRequestService;
        }

        public async Task OnGetAsync(string paymentIds, bool isApprove)
        {
            IsApproval = isApprove;
            SelectedPaymentIds = JsonSerializer.Deserialize<List<Guid>>(paymentIds) ?? [];
            var payments = await _paymentRequestService.GetListByPaymentIdsAsync(SelectedPaymentIds);

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
                };

                ApplicationPaymentApprovalForm!.Add(request);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ApplicationPaymentApprovalForm == null) return NoContent();

            if (IsApproval)
            {
                Status = PaymentRequestStatus.L1Approved;
            }
            else
            {
                Status = PaymentRequestStatus.L1Declined;
            }
            var payments = MapPaymentRequests(Status);

            await _paymentRequestService.UpdateStatusAsync(payments);

            return NoContent();
        }

        private List<UpdatePaymentStatusRequestDto> MapPaymentRequests(PaymentRequestStatus status)
        {
            var payments = new List<UpdatePaymentStatusRequestDto>();

            if (ApplicationPaymentApprovalForm == null) return payments;

            foreach (var payment in ApplicationPaymentApprovalForm)
            {
                payments.Add(new UpdatePaymentStatusRequestDto()
                {
                    PaymentRequestId = payment.Id,
                    Status = status,

                });
            }

            return payments;
        }
    }
}
