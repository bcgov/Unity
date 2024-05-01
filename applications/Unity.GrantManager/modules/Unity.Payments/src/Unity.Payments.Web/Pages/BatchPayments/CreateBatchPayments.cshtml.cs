using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Unity.Payments.Suppliers;
using Unity.Payments.BatchPaymentRequests;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.Payments.PaymentConfigurations;
using Unity.GrantManager.GrantApplications;
using Microsoft.AspNetCore.Mvc.Rendering;
using Unity.Payment.Shared;
using System.Text.Json;

namespace Unity.Payments.Web.Pages.BatchPayments
{
    public class CreateBatchPaymentsModel : AbpPageModel
    {
        [BindProperty]
        public List<BatchPaymentsModel> ApplicationPaymentRequestForm { get; set; } = [];

        [BindProperty]
        public decimal PaymentThreshold { get; set; }

        public List<Guid> SelectedApplicationIds { get; set; }

        private readonly IGrantApplicationAppService _applicationService;

        private readonly IBatchPaymentRequestAppService _batchPaymentRequestService;
        private readonly IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly ISupplierAppService _iSupplierAppService;

        public CreateBatchPaymentsModel(IGrantApplicationAppService applicationService,
           ISupplierAppService iSupplierAppService,
           IBatchPaymentRequestAppService batchPaymentRequestService,
           IPaymentConfigurationAppService paymentConfigurationAppService)
        {
            SelectedApplicationIds = [];
            _applicationService = applicationService;
            _batchPaymentRequestService = batchPaymentRequestService;
            _paymentConfigurationAppService = paymentConfigurationAppService;
            _iSupplierAppService = iSupplierAppService;
        }

        public async Task OnGetAsync(string applicationIds)
        {
            SelectedApplicationIds = JsonSerializer.Deserialize<List<Guid>>(applicationIds) ?? [];
            var applications = await _applicationService.GetApplicationDetailsListAsync(SelectedApplicationIds);

            foreach (var application in applications)
            {
                BatchPaymentsModel request = new()
                {
                    ApplicationId = application.Id,
                    ApplicantName = application.Applicant.ApplicantName == "" ? "Applicant Name" : application.Applicant.ApplicantName,
                    Amount = application.ApprovedAmount,
                    Description = "",
                    InvoiceNumber = application.ReferenceNo,
                    ContractNumber = application.ContractNumber,
                    SupplierNumber = application.ContractNumber,
                };

                // Massage Site list
                var supplier = await _iSupplierAppService.GetByCorrelationAsync(new GetSupplierByCorrelationDto()
                {
                    CorrelationId = application.Applicant.Id,
                    CorrelationProvider = GrantManager.Payments.PaymentConsts.ApplicantCorrelationProvider,
                    IncludeDetails = true
                });

                // If there are sites then add them
                if (supplier != null
                    && supplier.Sites != null
                    && supplier.Sites.Count > 0
                    && supplier.Number != null)
                {
                    string supplierNumber = supplier.Number;
                    foreach (var site in supplier.Sites)
                    {
                        SelectListItem item = new SelectListItem
                        {
                            Value = site.Id.ToString(),
                            Text = $"{site.Number} ({supplierNumber}, {site.City})",
                        };
                        request.SiteList.Add(item);
                    }
                }

                ApplicationPaymentRequestForm!.Add(request);
            }

            var paymentConfiguration = await _paymentConfigurationAppService.GetAsync();
            PaymentThreshold = paymentConfiguration?.PaymentThreshold ?? PaymentSharedConsts.DefaultThresholdAmount;
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
                    SiteId = payment.SiteId,
                    Description = payment.Description,
                    InvoiceNumber = payment.InvoiceNumber,
                    ContractNumber = payment.ContractNumber ?? string.Empty,
                    SupplierNumber = payment.ContractNumber ?? string.Empty,
                    PayeeName = payment.ApplicantName ?? string.Empty,

                });
            }

            return payments;
        }
    }
}
