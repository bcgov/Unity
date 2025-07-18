using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Unity.Payments.Suppliers;
using Unity.Payments.PaymentRequests;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.Payments.PaymentConfigurations;
using Unity.GrantManager.GrantApplications;
using Unity.Payment.Shared;
using System.Text.Json;
using Unity.Payments.Domain.Suppliers;
using System.Linq;
using Unity.GrantManager.Payments;

namespace Unity.Payments.Web.Pages.Payments
{
    public class CreatePaymentRequestsModel : AbpPageModel
    {
        public List<Guid> SelectedApplicationIds { get; set; }
        private readonly IGrantApplicationAppService _applicationService;
        private readonly IPaymentRequestAppService _paymentRequestService;
        private readonly IPaymentConfigurationAppService _paymentConfigurationAppService;
        private readonly ISupplierAppService _iSupplierAppService;
        private readonly ISiteRepository _siteRepository;
        private readonly IPaymentSettingsAppService _paymentSettingsAppService;

        public CreatePaymentRequestsModel(
           IPaymentSettingsAppService paymentSettingsAppService,
           ISiteRepository siteRepository,
           IGrantApplicationAppService applicationService,
           ISupplierAppService iSupplierAppService,
           IPaymentRequestAppService paymentRequestService,
           IPaymentConfigurationAppService paymentConfigurationAppService)
        {
            SelectedApplicationIds = [];
            _siteRepository = siteRepository;
            _applicationService = applicationService;
            _paymentRequestService = paymentRequestService;
            _paymentConfigurationAppService = paymentConfigurationAppService;
            _iSupplierAppService = iSupplierAppService;
            _paymentSettingsAppService = paymentSettingsAppService;
        }

        [BindProperty]
        public List<PaymentsModel> ApplicationPaymentRequestForm { get; set; } = [];
        [BindProperty]
        public decimal PaymentThreshold { get; set; }
        [BindProperty]
        public bool DisableSubmit { get; set; }
        [BindProperty]
        public bool HasPaymentConfiguration { get; set; }

        [BindProperty]
        public string BatchNumberDisplay { get; set; } = string.Empty;


        [BindProperty]
        public decimal TotalAmount { get; set; }

        public decimal ApplicationPaymentRequestFormTotalAmount
        {
            get
            {
                return ApplicationPaymentRequestForm?.Sum(x => x.Amount) ?? 0m;
            }
        }

        public async Task OnGetAsync(string applicationIds)
        {
            // TODO: FIX PAY THRESHOLD
            var paymentConfiguration = await _paymentConfigurationAppService.GetAsync();
            if (paymentConfiguration != null)
            {
                PaymentThreshold = PaymentSharedConsts.DefaultThresholdAmount;
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

                // Grabs the Account Coding ID from the Application Form and if there is none then the Payment Configuration
                // If neither exist then an error on the payment request will be shown 
                Guid? accountCodingId = await _paymentSettingsAppService.GetAccountCodingIdByApplicationIdAsync(application.Id);

                PaymentsModel request = new()
                {
                    CorrelationId = application.Id,
                    ApplicantName = application.Applicant.ApplicantName == "" ? "Applicant Name" : application.Applicant.ApplicantName,
                    SubmissionConfirmationCode = application.ReferenceNo,
                    Amount = remainingAmount,
                    Description = "",
                    InvoiceNumber = application.ReferenceNo,
                    ContractNumber = application.ContractNumber,
                    RemainingAmount = remainingAmount,
                    AccountCodingId = accountCodingId
                };

                var supplier = await GetSupplierByApplicationAync(application);
                string supplierNumber = supplier?.Number?? string.Empty;

                Guid siteId = application.Applicant.SiteId;
                Site? site = null;
                if(siteId != Guid.Empty) {
                    site = await _siteRepository.GetAsync(siteId);
                    var siteName = $"{site.Number} ({supplierNumber}, {site.City})";
                    request.SiteName = siteName;
                    request.SiteId = siteId;
                }

                request.SupplierName = supplier?.Name;
                request.SupplierNumber = supplierNumber;

                request.ErrorList = GetErrorlist(supplier, site, application, remainingAmount, accountCodingId);

                if (request.ErrorList != null && request.ErrorList.Count > 0)
                {
                    request.DisableFields = true;
                }

                ApplicationPaymentRequestForm!.Add(request);
            }

            var batchName = await _paymentRequestService.GetNextBatchInfoAsync();
            BatchNumberDisplay = batchName;
            TotalAmount = ApplicationPaymentRequestForm?.Sum(x => x.Amount) ?? 0m;
        }

        private static List<string> GetErrorlist(SupplierDto? supplier, Site? site, GrantApplicationDto application, decimal remainingAmount, Guid? accountCodingId)
        {
            bool missingFields = false;

            List<string> errorList = [];
            if (supplier == null || site == null || supplier.Number == null)
            {
                missingFields = true;
            }

            if (remainingAmount <= 0)
            {
                errorList.Add("There is no remaining amount for this application.");
            }

            if (missingFields)
            {
                errorList.Add("Some payment information is missing for this applicant, please make sure Supplier info is provided and default site is selected.");
            }

            if (application.StatusCode != GrantApplicationState.GRANT_APPROVED)
            {
                errorList.Add("The selected Application is not Approved. To continue please remove the item from the list.");
            }

            if (!application.ApplicationForm.Payable)
            {
                errorList.Add("The selected application is not Payable. To continue please remove the item from the list.");
            }

            if(accountCodingId == null || accountCodingId == Guid.Empty)
            {
                errorList.Add("The selected application form does not have an Account Coding or no default Account Coding is set.");
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
            if(application.Applicant.SupplierId != Guid.Empty)
            {
                SupplierDto? supplierDto =  await _iSupplierAppService.GetAsync(application.Applicant.SupplierId);
                if (supplierDto != null)
                {
                    return supplierDto;
                }
            }

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
                    SupplierName = payment.SupplierName ?? string.Empty,
                    SupplierNumber = payment.SupplierNumber ?? string.Empty,
                    PayeeName = payment.ApplicantName ?? string.Empty,
                    SubmissionConfirmationCode = payment.SubmissionConfirmationCode ?? string.Empty,
                    CorrelationProvider = PaymentConsts.ApplicationCorrelationProvider,
                    AccountCodingId = payment.AccountCodingId,
                });
            }

            return payments;
        }
    }
}
