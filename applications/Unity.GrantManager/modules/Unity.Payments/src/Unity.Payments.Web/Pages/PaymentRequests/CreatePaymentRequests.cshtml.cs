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
           IPaymentRequestAppService paymentRequestService,
           IPaymentConfigurationAppService paymentConfigurationAppService)
        {
            SelectedApplicationIds = [];
            _applicationService = applicationService;
            _paymentRequestService = paymentRequestService;
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
            }
            else
            {
                DisableSubmit = true;
                HasPaymentConfiguration = false;
            }

            SelectedApplicationIds = JsonSerializer.Deserialize<List<Guid>>(applicationIds) ?? [];
            var applications = await _applicationService.GetApplicationDetailsListAsync(SelectedApplicationIds);

            foreach (var application in applications)
            {
                decimal remainingAmount = await GetRemainingAmountAllowedByApplicationAsync(application);

                PaymentsModel request = new()
                {
                    CorrelationId = application.Id,
                    ApplicantName = application.Applicant.ApplicantName == "" ? "Applicant Name" : application.Applicant.ApplicantName,
                    Amount = remainingAmount,
                    Description = "",
                    InvoiceNumber = application.ReferenceNo,
                    ContractNumber = application.ContractNumber,
                    RemainingAmount = remainingAmount
                };

                var supplier = await GetSupplierByApplicationAync(application);

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
                }

                request.ErrorList = GetErrorlist(supplier, application, remainingAmount);

                if (request.ErrorList != null && request.ErrorList.Count > 0)
                {
                    request.DisableFields = true;
                }

                ApplicationPaymentRequestForm!.Add(request);
            }

        }

        private static List<string> GetErrorlist(SupplierDto? supplier, GrantApplicationDto application, decimal remainingAmount)
        {
            bool missingFields = false;
            List<string> errorList = [];
            if (!(supplier != null
                                && supplier.Sites != null
                                && supplier.Sites.Count > 0
                                && supplier.Number != null))
            {
                missingFields = true;
            }

            if (application.ContractNumber.IsNullOrEmpty())
            {
                missingFields = true;
            }

            if (remainingAmount <= 0)
            {
                errorList.Add("There is no remaining amount for this application.");
            }

            if (missingFields)
            {
                errorList.Add("Some payment information is missing for this applicant, please make sure Contract # and Supplier info are available.");
            }

            if (application.StatusCode != GrantApplicationState.GRANT_APPROVED)
            {
                errorList.Add("The selected Application is not Approved. To continue please remove the item from the list.");
            }

            if (!application.ApplicationForm.Payable)
            {
                errorList.Add("The selected application is not Payable. To continue please remove the item from the list.");
            }
            return errorList;
        }

        private async Task<decimal> GetRemainingAmountAllowedByApplicationAsync(GrantApplicationDto application)
        {
            decimal remainingAmount = 0;
            // Calculate the "Future paid amount" and if it is more than Approved Amount, the system shall:
            // Highlight the record
            // Show error message: This payment exceeds the Approved Amount.
            // Future paid amount: Total Pending Amount + Total Paid amount + Amount that is in the current payment request
            if (application.ApprovedAmount > 0)
            {
                decimal approvedAmmount = application.ApprovedAmount;
                decimal totalFutureRequested = await _paymentRequestService.GetTotalPaymentRequestAmountByCorrelationIdAsync(application.Id);
                if (approvedAmmount > totalFutureRequested)
                {
                    remainingAmount = approvedAmmount - totalFutureRequested;
                }
            }

            return remainingAmount;
        }

        private async Task<SupplierDto?> GetSupplierByApplicationAync(GrantApplicationDto application)
        {
            return await _iSupplierAppService.GetByCorrelationAsync(new GetSupplierByCorrelationDto()
            {
                CorrelationId = application.Applicant.Id,
                CorrelationProvider = GrantManager.Payments.PaymentConsts.ApplicantCorrelationProvider,
                IncludeDetails = true
            });
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
                    CorrelationId = payment.CorrelationId,
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
