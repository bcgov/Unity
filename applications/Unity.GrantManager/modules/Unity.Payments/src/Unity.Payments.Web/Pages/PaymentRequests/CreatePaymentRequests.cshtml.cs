using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Unity.Payments.Suppliers;
using Unity.Payments.PaymentRequests;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.Payments.PaymentConfigurations;
using Unity.GrantManager.GrantApplications;
using System.Text.Json;
using Unity.Payments.Domain.Suppliers;
using System.Linq;
using Unity.GrantManager.Payments;
using Unity.GrantManager.Applications;
using Unity.GrantManager.ApplicationForms;

namespace Unity.Payments.Web.Pages.Payments
{
    public class CreatePaymentRequestsModel(
        IGrantApplicationAppService applicationService,
        IPaymentRequestAppService paymentRequestAppService,
        IPaymentConfigurationAppService paymentConfigurationAppService,
        ISupplierAppService iSupplierAppService,
        ISiteRepository siteRepository,
        IPaymentSettingsAppService paymentSettingsAppService,
        IApplicationLinksService applicationLinksService,
        IApplicationFormRepository applicationFormRepository,
        IApplicationFormAppService applicationFormAppService
    ) : AbpPageModel
    {

        public List<Guid> SelectedApplicationIds { get; set; } = [];

        [BindProperty]
        public List<PaymentsModel> ApplicationPaymentRequestForm { get; set; } = [];

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
            var paymentConfiguration = await paymentConfigurationAppService.GetAsync();
            if (paymentConfiguration != null)
            {
                HasPaymentConfiguration = true;
            }
            else
            {
                DisableSubmit = true;
                HasPaymentConfiguration = false;
            }

            SelectedApplicationIds = JsonSerializer.Deserialize<List<Guid>>(applicationIds) ?? [];
            var applications = await applicationService.GetApplicationDetailsListAsync(SelectedApplicationIds);

            foreach (var application in applications)
            {
                decimal remainingAmount = await GetRemainingAmountAllowedByApplicationAsync(application);

                // Grabs the Account Coding ID from the Application Form and if there is none then the Payment Configuration
                // If neither exist then an error on the payment request will be shown
                Guid? accountCodingId = await paymentSettingsAppService.GetAccountCodingIdByApplicationIdAsync(application.Id);

                // Load ApplicationForm with hierarchy information
                var applicationForm = await applicationFormRepository.GetAsync(application.ApplicationForm.Id);

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
                    site = await siteRepository.GetAsync(siteId);
                    var siteName = $"{site.Number} ({supplierNumber}, {site.City})";
                    request.SiteName = siteName;
                    request.SiteId = siteId;
                }

                request.SupplierName = supplier?.Name;
                request.SupplierNumber = supplierNumber;

                var (errorList, parentReferenceNo) = await GetErrorlist(supplier, site, application, applicationForm, remainingAmount, accountCodingId);
                request.ErrorList = errorList;
                request.ParentReferenceNo = parentReferenceNo;

                if (request.ErrorList != null && request.ErrorList.Count > 0)
                {
                    request.DisableFields = true;
                }

                ApplicationPaymentRequestForm!.Add(request);
            }

            ApplicationPaymentRequestForm = SortPaymentRequestsByHierarchy(ApplicationPaymentRequestForm);

            var batchName = await paymentRequestAppService.GetNextBatchInfoAsync();
            BatchNumberDisplay = batchName;
            TotalAmount = ApplicationPaymentRequestForm?.Sum(x => x.Amount) ?? 0m;
        }

        private async Task<(List<string> ErrorList, string? ParentReferenceNo)> GetErrorlist(SupplierDto? supplier, Site? site, GrantApplicationDto application, ApplicationForm applicationForm, decimal remainingAmount, Guid? accountCodingId)
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

            // Add form hierarchy and parent link validation
            var (hierarchyErrors, parentReferenceNo) = await ValidateFormHierarchyAndParentLink(application, applicationForm);
            errorList.AddRange(hierarchyErrors);

            return (errorList, parentReferenceNo);
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
                decimal totalFutureRequested = await paymentRequestAppService.GetTotalPaymentRequestAmountByCorrelationIdAsync(application.Id);
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
                SupplierDto? supplierDto =  await iSupplierAppService.GetAsync(application.Applicant.SupplierId);
                if (supplierDto != null)
                {
                    return supplierDto;
                }
            }

            return await iSupplierAppService.GetByCorrelationAsync(new GetSupplierByCorrelationDto()
            {
                CorrelationId = application.Applicant.Id,
                CorrelationProvider = GrantManager.Payments.PaymentConsts.ApplicantCorrelationProvider,
                IncludeDetails = true
            });
        }

        private async Task<(List<string> Errors, string? ParentReferenceNo)> ValidateFormHierarchyAndParentLink(
            GrantApplicationDto application,
            ApplicationForm applicationForm)
        {
            List<string> errors = [];
            string? parentReferenceNo = null;

            // Only validate if form is payable and has Child hierarchy
            if (!applicationForm.Payable ||
                !applicationForm.FormHierarchy.HasValue ||
                applicationForm.FormHierarchy.Value != FormHierarchyType.Child)
            {
                return (errors, parentReferenceNo); // No validation needed
            }

            // Check if ParentFormId and ParentFormVersionId are set
            if (!applicationForm.ParentFormId.HasValue ||
                !applicationForm.ParentFormVersionId.HasValue)
            {
                // Configuration issue - should not happen if validation works
                return (errors, parentReferenceNo);
            }

            // Get parent links for this application
            var allLinks = await applicationLinksService.GetListByApplicationAsync(application.Id);
            var parentLink = allLinks.Find(link => link.LinkType == ApplicationLinkType.Parent);

            // Rule 2: No parent link exists
            if (parentLink == null)
            {
                errors.Add("Payment Configuration for this form requires a valid parent application link before payments can be processed.");
                return (errors, parentReferenceNo);
            }

            // Rule 1: Parent link exists but doesn't match Payment Configuration
            // Get the parent application's form version details
            var parentFormDetails = await applicationFormAppService.GetFormDetailsByApplicationIdAsync(parentLink.ApplicationId);

            // If validation passed, get the parent application's reference number
            var parentApplication = await applicationService.GetAsync(parentLink.ApplicationId);
            parentReferenceNo = parentApplication.ReferenceNo;

            // Validate both ParentFormId and ParentFormVersionId
            bool formIdMatches = parentFormDetails.ApplicationFormId == applicationForm.ParentFormId.Value;
            bool versionIdMatches = parentFormDetails.ApplicationFormVersionId == applicationForm.ParentFormVersionId.Value;

            if (!formIdMatches || !versionIdMatches)
            {
                errors.Add("The selected parent form in Payment Configuration does not match the application's linked parent. Please verify and try again.");
                return (errors, parentReferenceNo);
            }            

            return (errors, parentReferenceNo);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ApplicationPaymentRequestForm == null) return NoContent();

            var payments = MapPaymentRequests();

            await paymentRequestAppService.CreateAsync(payments);

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

        private List<PaymentsModel> SortPaymentRequestsByHierarchy(List<PaymentsModel> paymentRequests)
        {
            var sortedList = new List<PaymentsModel>();
            var processed = new HashSet<string>();

            // Step 1: Find all parent-child groups and process them
            var parentGroups = paymentRequests
                .Where(x => !string.IsNullOrEmpty(x.ParentReferenceNo))
                .GroupBy(x => x.ParentReferenceNo)
                .ToList();

            foreach (var group in parentGroups.OrderBy(g => g.Key))
            {
                string parentRefNo = group.Key!;

                // Add children first (sorted by InvoiceNumber)
                var children = group.OrderBy(x => x.InvoiceNumber).ToList();
                sortedList.AddRange(children);
                foreach (var child in children)
                {
                    processed.Add(child.InvoiceNumber);
                }

                // Add parent after children (if it exists in the list)
                var parent = paymentRequests
                    .FirstOrDefault(x => x.InvoiceNumber == parentRefNo && string.IsNullOrEmpty(x.ParentReferenceNo));

                if (parent != null)
                {
                    sortedList.Add(parent);
                    processed.Add(parent.InvoiceNumber);
                }
            }

            // Step 2: Add standalone items at the end
            var standaloneItems = paymentRequests
                .Where(x => !processed.Contains(x.InvoiceNumber))
                .OrderBy(x => x.InvoiceNumber)
                .ToList();
            sortedList.AddRange(standaloneItems);

            return sortedList;
        }
    }
}
