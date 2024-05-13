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

namespace Unity.Payments.Web.Pages.Payments
{
    public class CreatePaymentRequestsModel : AbpPageModel
    {
        [BindProperty]
        public List<PaymentsModel> ApplicationPaymentRequestForm { get; set; } = [];

        [BindProperty]
        public decimal PaymentThreshold { get; set; }
        
        [BindProperty]
        public bool DisableSubmit { get; set; }

        [BindProperty]
        public bool HasPaymentConfiguration { get; set; }

        public List<Guid> SelectedApplicationIds { get; set; }

        private readonly IGrantApplicationAppService _applicationService;

        private readonly IPaymentRequestAppService _paymentRequestService;
        private readonly IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly ISupplierAppService _iSupplierAppService;

        public CreatePaymentRequestsModel(IGrantApplicationAppService applicationService,
           ISupplierAppService iSupplierAppService,
           IPaymentRequestAppService batchPaymentRequestService,
           IPaymentConfigurationAppService paymentConfigurationAppService)
        {
            SelectedApplicationIds = [];
            _applicationService = applicationService;
            _paymentRequestService = batchPaymentRequestService;
            _paymentConfigurationAppService = paymentConfigurationAppService;
            _iSupplierAppService = iSupplierAppService;
        }

        public async Task OnGetAsync(string applicationIds)
        {
            var paymentConfiguration = await _paymentConfigurationAppService.GetAsync();
            if (paymentConfiguration != null)
            {
                PaymentThreshold = paymentConfiguration?.PaymentThreshold ?? PaymentSharedConsts.DefaultThresholdAmount;
                HasPaymentConfiguration = true;
            } else
            {
                DisableSubmit = true;
                HasPaymentConfiguration = false;
            }

            SelectedApplicationIds = JsonSerializer.Deserialize<List<Guid>>(applicationIds) ?? [];
            var applications = await _applicationService.GetApplicationDetailsListAsync(SelectedApplicationIds);

            foreach (var application in applications)
            {
                List<string> errorList = [];
                bool missingFields = false;

                PaymentsModel request = new()
                {
                    ApplicationId = application.Id,
                    ApplicantName = application.Applicant.ApplicantName == "" ? "Applicant Name" : application.Applicant.ApplicantName,
                    Amount = decimal.Parse("0.00"),
                    Description = "",
                    InvoiceNumber = application.ReferenceNo,
                    ContractNumber = application.ContractNumber,
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
                    request.SupplierNumber = supplier.Number;
                    foreach (var site in supplier.Sites)
                    {
                        SelectListItem item = new()
                        {
                            Value = site.Id.ToString(),
                            Text = $"{site.Number} ({supplierNumber}, {site.City})",
                        };
                        request.SiteList.Add(item);
                    }
                } else {
                    missingFields = true;
                }

                if(application.ContractNumber.IsNullOrEmpty())
                {
                    missingFields = true;
                }

                if(missingFields)
                {
                    errorList.Add("Some payment information is missing for this applicant, please make sure Contract # and Supplier info are available.");
                    request.DisableFields = true;
                }

                if (application.StatusCode != GrantApplicationState.GRANT_APPROVED) {
                    errorList.Add("The selected Application is not Approved. To continue please remove the item from the list.");
                    request.DisableFields = true;
                }

                if(!application.ApplicationForm.Payable) {
                    errorList.Add("The selected application is not Payable. To continue please remove the item from the list.");
                    request.DisableFields = true;
                }

                request.ErrorList = errorList;
                ApplicationPaymentRequestForm!.Add(request);
            }

        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ApplicationPaymentRequestForm == null) return NoContent();

            var payments = MapPaymentRequests();

            await _paymentRequestService.CreateAsync(payments);

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
                    SupplierNumber = payment.SupplierNumber ?? string.Empty,
                    PayeeName = payment.ApplicantName ?? string.Empty,
                    CorrelationProvider = GrantManager.Payments.PaymentConsts.ApplicationCorrelationProvider,

                });
            }

            return payments;
        }
    }
}
