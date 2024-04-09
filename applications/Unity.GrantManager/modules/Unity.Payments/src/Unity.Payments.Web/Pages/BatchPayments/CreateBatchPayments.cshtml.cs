using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.BatchPaymentRequests;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.Payments.Settings;

namespace Unity.Payments.Web.Pages.BatchPayments
{
    public class CreateBatchPaymentsModel : AbpPageModel
    {
        [BindProperty]
        public List<BatchPaymentsModel> ApplicationPaymentRequestForm { get; set; } = new();
        public List<Guid> SelectedApplicationIds { get; set; }
        public PaymentsSettingsDto Settings { get; set; } = new PaymentsSettingsDto();

        private readonly GrantApplicationAppService _applicationService;
        private readonly IBatchPaymentRequestAppService _batchPaymentRequestService;
        private readonly IPaymentsSettingsAppService _paymentsSettingsAppService;

        public CreateBatchPaymentsModel(GrantApplicationAppService applicationService,
           IBatchPaymentRequestAppService batchPaymentRequestService,
           IPaymentsSettingsAppService paymentsSettingsAppService)
        {
            SelectedApplicationIds = new();
            _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
            _batchPaymentRequestService = batchPaymentRequestService;
            _paymentsSettingsAppService = paymentsSettingsAppService;
        }

        public async void OnGet(string applicationIds)
        {
            SelectedApplicationIds = JsonConvert.DeserializeObject<List<Guid>>(applicationIds) ?? new List<Guid>();
            var applications = await _applicationService.GetApplicationDetailsListAsync(SelectedApplicationIds);
            Settings = await _paymentsSettingsAppService.GetAsync();
            foreach (var application in applications)
            {
                BatchPaymentsModel request = new()
                {
                    ApplicationId = application.Id,
                    ApplicantName = application.Applicant.ApplicantName == "" ? "Applicant Name" : application.Applicant.ApplicantName,
                    Amount = application.ApprovedAmount,
                    Description = "",
                    InvoiceNumber = application.ReferenceNo,
                };

                ApplicationPaymentRequestForm!.Add(request);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ApplicationPaymentRequestForm == null) return NoContent();

            await _batchPaymentRequestService.CreateAsync(new CreateBatchPaymentRequestDto()
            {
                Description = "Description",
                PaymentRequests = MapPaymentRequests(),
                Provider = "A"
            });

            return NoContent();
        }

        private List<CreatePaymentRequestDto> MapPaymentRequests()
        {
            var payments = new List<CreatePaymentRequestDto>();

            if (ApplicationPaymentRequestForm == null) return payments;

            foreach (var payment in ApplicationPaymentRequestForm)
            {
                payments.Add(new CreatePaymentRequestDto()
                {
                    Amount = payment.Amount,
                    CorrelationId = payment.ApplicationId,
                    Description = payment.Description,
                    InvoiceNumber = payment.InvoiceNumber
                });
            }

            return payments;
        }
    }
}
