using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.BatchPaymentRequests;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.Payments.Web.Pages.BatchPayments
{
    public class CreateBatchPaymentsModel : AbpPageModel
    {
        [BindProperty]
        public List<BatchPaymentsModel>? ApplicationPaymentRequestForm { get; set; } = new List<BatchPaymentsModel>();
        public List<Guid> SelectedApplicationIds { get; set; }

        private readonly GrantApplicationAppService _applicationService;
        private readonly IBatchPaymentRequestAppService _batchPaymentRequestService;

        public CreateBatchPaymentsModel(GrantApplicationAppService applicationService,
       IBatchPaymentRequestAppService batchPaymentRequestService)
        {
            SelectedApplicationIds = new List<Guid>();
            _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
            _batchPaymentRequestService = batchPaymentRequestService;
        }

        public async void OnGet(string applicationIds)
        {
            SelectedApplicationIds = JsonConvert.DeserializeObject<List<Guid>>(applicationIds) ?? new List<Guid>();
            var applications = await _applicationService.GetApplicationListAsync(SelectedApplicationIds);

            foreach (var application in applications)
            {
                BatchPaymentsModel request = new()
                {
                    ApplicationId = application.Id,
                    ApplicantName = application.Applicant.ApplicantName == "" ? "Applicant Name" : application.Applicant.ApplicantName,
                    Amount = 0,
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
