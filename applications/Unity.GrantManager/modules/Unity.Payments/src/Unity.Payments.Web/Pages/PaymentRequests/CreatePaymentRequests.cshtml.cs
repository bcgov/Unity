using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using Unity.Payments.Suppliers;
using Unity.Payments.PaymentRequests;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.Payments.PaymentConfigurations;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.Domain.Suppliers;
using System.Linq;
using Unity.GrantManager.Payments;
using Unity.GrantManager.Applications;
using Unity.GrantManager.ApplicationForms;
using Volo.Abp;
using Unity.Payments.Enums;
using Microsoft.Extensions.Logging;

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
        IApplicationFormAppService applicationFormAppService,
        ApplicationIdsCacheService cacheService
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

        public async Task OnGetAsync(string cacheKey)
        {
            try
            {
                var applicationIds = await cacheService.GetApplicationIdsAsync(cacheKey);

                if (applicationIds == null || applicationIds.Count == 0)
                {
                    Logger.LogWarning("Cache key expired or invalid: {CacheKey}", cacheKey);
                    ViewData["Error"] = "The session has expired. Please select applications and try again.";
                    DisableSubmit = true;
                    return;
                }

                SelectedApplicationIds = applicationIds;
                Logger.LogInformation("Successfully loaded payment requests modal for {Count} applications", applicationIds.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading payment requests modal");
                ViewData["Error"] = "An error occurred while loading the payment requests. Please try again.";
                DisableSubmit = true;
                return;
            }

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

            // Populate parent-child validation data for frontend
            await PopulateParentChildValidationData();

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

            // If the site paygroup is eft but there is no bank account
            if(site != null && site.PaymentGroup == PaymentGroup.EFT && string.IsNullOrWhiteSpace(site.BankAccount)) 
            {
                errorList.Add("Payment cannot be submitted because the default site’s pay group is set to EFT, but no bank account is configured. Please update the bank account before proceeding.");
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

                // If this application has children, include their paid/pending amounts too
                decimal childrenTotalPaidPending = 0;
                var applicationLinks = await applicationLinksService.GetListByApplicationAsync(application.Id);
                var childLinks = applicationLinks.Where(link => link.LinkType == ApplicationLinkType.Child).ToList();

                if (childLinks.Count > 0)
                {
                    // This is a parent application, sum up all children's paid/pending payments
                    foreach (var childLink in childLinks)
                    {
                        decimal childTotal = await paymentRequestAppService
                            .GetTotalPaymentRequestAmountByCorrelationIdAsync(childLink.ApplicationId);
                        childrenTotalPaidPending += childTotal;
                    }
                }

                // Calculate remaining: Approved - (Parent Paid/Pending + Children Paid/Pending)
                decimal totalConsumed = totalFutureRequested + childrenTotalPaidPending;
                if (approvedAmmount > totalConsumed)
                {
                    remainingAmount = approvedAmmount - totalConsumed;
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

            // Check if ParentFormId is set
            if (!applicationForm.ParentFormId.HasValue)
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

            // Validate ParentFormId matches
            bool formIdMatches = parentFormDetails.ApplicationFormId == applicationForm.ParentFormId.Value;

            if (!formIdMatches)
            {
                errors.Add("The selected parent form in Payment Configuration does not match the application's linked parent. Please verify and try again.");
                return (errors, parentReferenceNo);
            }            

            return (errors, parentReferenceNo);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ApplicationPaymentRequestForm == null) return NoContent();

            // Validate parent-child payment amounts
            var validationErrors = await ValidateParentChildPaymentAmounts();
            if (validationErrors.Count != 0)
            {
                throw new UserFriendlyException(string.Join(" ", validationErrors));
            }

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

        private static List<PaymentsModel> SortPaymentRequestsByHierarchy(List<PaymentsModel> paymentRequests)
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
                    .Find(x => x.SubmissionConfirmationCode == parentRefNo && string.IsNullOrEmpty(x.ParentReferenceNo));

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

        private async Task PopulateParentChildValidationData()
        {
            // Find all child groups in current submission
            var childGroups = ApplicationPaymentRequestForm
                .Where(x => !string.IsNullOrEmpty(x.ParentReferenceNo))
                .GroupBy(x => x.ParentReferenceNo);

            foreach (var childGroup in childGroups)
            {
                string parentRefNo = childGroup.Key!;
                var children = childGroup.ToList();

                // Find parent in current submission
                var parentInSubmission = ApplicationPaymentRequestForm
                    .Find(x => x.SubmissionConfirmationCode == parentRefNo &&
                                         string.IsNullOrEmpty(x.ParentReferenceNo));

                // Get parent application details
                Guid parentApplicationId;

                if (parentInSubmission != null)
                {
                    parentApplicationId = parentInSubmission.CorrelationId;
                }
                else
                {
                    // Parent not in submission, get from first child's link
                    var firstChild = await applicationService.GetAsync(children[0].CorrelationId);
                    var allLinks = await applicationLinksService.GetListByApplicationAsync(firstChild.Id);
                    var parentLink = allLinks.Find(link => link.LinkType == ApplicationLinkType.Parent);

                    if (parentLink == null)
                    {
                        // Skip this group if parent link not found
                        continue;
                    }
                    parentApplicationId = parentLink.ApplicationId;
                }

                // Get parent application
                var parentApplication = await applicationService.GetAsync(parentApplicationId);
                decimal approvedAmount = parentApplication.ApprovedAmount;

                // Get parent's total paid + pending
                decimal parentTotalPaidPending = await paymentRequestAppService
                    .GetTotalPaymentRequestAmountByCorrelationIdAsync(parentApplicationId);

                // Get ALL children of this parent and their total paid + pending
                var parentLinks = await applicationLinksService.GetListByApplicationAsync(parentApplicationId);
                var allChildLinks = parentLinks.Where(link => link.LinkType == ApplicationLinkType.Child).ToList();

                decimal childrenTotalPaidPending = 0;
                foreach (var childLink in allChildLinks)
                {
                    decimal childTotal = await paymentRequestAppService
                        .GetTotalPaymentRequestAmountByCorrelationIdAsync(childLink.ApplicationId);
                    childrenTotalPaidPending += childTotal;
                }

                // Calculate maximum allowed
                decimal maximumPaymentAmount = approvedAmount - (parentTotalPaidPending + childrenTotalPaidPending);

                // Apply validation data to all children in this group
                foreach (var child in children)
                {
                    child.MaximumAllowedAmount = maximumPaymentAmount;
                    child.IsPartOfParentChildGroup = true;
                    child.ParentApprovedAmount = approvedAmount;
                }

                // Apply validation data to parent if in submission
                if (parentInSubmission != null)
                {
                    parentInSubmission.MaximumAllowedAmount = maximumPaymentAmount;
                    parentInSubmission.IsPartOfParentChildGroup = true;
                    parentInSubmission.ParentApprovedAmount = approvedAmount;
                }
            }
        }

        private async Task<List<string>> ValidateParentChildPaymentAmounts()
        {
            List<string> errors = [];

            // Find all child groups in current submission
            var childGroups = ApplicationPaymentRequestForm
                .Where(x => !string.IsNullOrEmpty(x.ParentReferenceNo))
                .GroupBy(x => x.ParentReferenceNo);

            foreach (var childGroup in childGroups)
            {
                string parentRefNo = childGroup.Key!;
                var children = childGroup.ToList();

                // Find parent in current submission
                var parentInSubmission = ApplicationPaymentRequestForm
                    .Find(x => x.SubmissionConfirmationCode == parentRefNo &&
                                         string.IsNullOrEmpty(x.ParentReferenceNo));

                // Get parent application details
                Guid parentApplicationId;
                decimal currentParentAmount = 0;

                if (parentInSubmission != null)
                {
                    parentApplicationId = parentInSubmission.CorrelationId;
                    currentParentAmount = parentInSubmission.Amount;
                }
                else
                {
                    // Parent not in submission, get from first child's link
                    var firstChild = await applicationService.GetAsync(children[0].CorrelationId);
                    var allLinks = await applicationLinksService.GetListByApplicationAsync(firstChild.Id);
                    var parentLink = allLinks.Find(link => link.LinkType == ApplicationLinkType.Parent);

                    if (parentLink == null)
                    {
                        errors.Add($"Parent application link not found for reference {parentRefNo}.");
                        continue;
                    }
                    parentApplicationId = parentLink.ApplicationId;
                }

                // Get parent application
                var parentApplication = await applicationService.GetAsync(parentApplicationId);
                decimal approvedAmount = parentApplication.ApprovedAmount;

                // Get parent's total paid + pending
                decimal parentTotalPaidPending = await paymentRequestAppService
                    .GetTotalPaymentRequestAmountByCorrelationIdAsync(parentApplicationId);

                // Get ALL children of this parent and their total paid + pending
                var parentLinks = await applicationLinksService.GetListByApplicationAsync(parentApplicationId);
                var allChildLinks = parentLinks.Where(link => link.LinkType == ApplicationLinkType.Child).ToList();

                decimal childrenTotalPaidPending = 0;
                foreach (var childLink in allChildLinks)
                {
                    decimal childTotal = await paymentRequestAppService
                        .GetTotalPaymentRequestAmountByCorrelationIdAsync(childLink.ApplicationId);
                    childrenTotalPaidPending += childTotal;
                }

                // Calculate maximum allowed
                decimal maximumPaymentAmount = approvedAmount - (parentTotalPaidPending + childrenTotalPaidPending);

                // Calculate current submission total
                decimal currentChildrenAmount = children.Sum(x => x.Amount);
                decimal currentSubmissionTotal = currentParentAmount + currentChildrenAmount;

                // Validate
                if (currentSubmissionTotal > maximumPaymentAmount)
                {
                    errors.Add($"Payment request for parent application {parentRefNo} and its children exceeds the maximum allowed amount. " +
                              $"Maximum: ${maximumPaymentAmount:N2}, Requested: ${currentSubmissionTotal:N2}. " +
                              $"(Parent Approved Amount: ${approvedAmount:N2}, Already Paid/Pending for Parent: ${parentTotalPaidPending:N2}, " +
                              $"Already Paid/Pending for All Children: ${childrenTotalPaidPending:N2})");
                }
            }

            return errors;
        }
    }
}
