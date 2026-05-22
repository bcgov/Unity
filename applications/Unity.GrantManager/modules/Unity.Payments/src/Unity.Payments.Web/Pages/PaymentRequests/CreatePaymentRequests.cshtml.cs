using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using Unity.Payments.PaymentRequests;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.Payments.PaymentConfigurations;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Applications;
using Volo.Abp;
using System.Linq;
using Microsoft.Extensions.Logging;
using Unity.Payments.Domain.AccountCodings;
using Microsoft.AspNetCore.Authorization;
using Unity.Payments.Permissions;
using Unity.Payments.Domain.Suppliers;
using Unity.GrantManager.Payments;


namespace Unity.Payments.Web.Pages.Payments
{
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Primary constructor for dependency injection")]
    public class CreatePaymentRequestsModel(
        IAccountCodingRepository accountCodingRepository,
        IGrantApplicationAppService applicationService,
        IPaymentRequestAppService paymentRequestAppService,
        IPaymentConfigurationAppService paymentConfigurationAppService,
        ISiteRepository siteRepository,
        IPaymentSettingsAppService paymentSettingsAppService,
        IApplicationFormRepository applicationFormRepository,
        ApplicationIdsCacheService cacheService,
        PaymentRequestPageHelperService helperService
    ) : AbpPageModel
    {

        public List<Guid> SelectedApplicationIds { get; set; } = [];

        public List<Domain.AccountCodings.AccountCoding> AccountCodings { get; private set; } = [];

        public Dictionary<Guid, string> AccountCodingDisplayMap { get; set; } = [];

        [BindProperty]
        public List<PaymentsModel> ApplicationPaymentRequestForm { get; set; } = [];

        [BindProperty]
        public bool DisableSubmit { get; set; }

        [BindProperty]
        public bool HasPaymentConfiguration { get; set; }

        [BindProperty]
        public string? DefaultAccountCodingId { get; set; }

        [BindProperty]
        public string? AccountCodingOverride { get; set; }

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
                DefaultAccountCodingId = paymentConfiguration.DefaultAccountCodingId != Guid.Empty ? paymentConfiguration.DefaultAccountCodingId.ToString() : null;
            }
            else
            {
                DisableSubmit = true;
                HasPaymentConfiguration = false;
            }
            AccountCodings = await accountCodingRepository.GetListAsync();
            AccountCodingDisplayMap = AccountCodings.ToDictionary(
                ac => ac.Id,
                ac => ac.FullAccountCode() + (ac.Id.ToString() == DefaultAccountCodingId ? " (Default)" : "")
            );
            var applications = await applicationService.GetApplicationDetailsListAsync(SelectedApplicationIds);

            foreach (var application in applications)
            {
                decimal remainingAmount = await helperService.GetRemainingAmountAsync(application);

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

                var supplier = await helperService.GetSupplierAsync(application);
                string supplierNumber = supplier?.Number ?? string.Empty;

                Guid siteId = application.DefaultSiteId ?? Guid.Empty;
                Site? site = null;
                if (siteId != Guid.Empty)
                {
                    site = await siteRepository.GetAsync(siteId);
                    var siteName = $"{site.Number} ({supplierNumber}, {site.City})";
                    request.SiteName = siteName;
                    request.SiteId = siteId;
                }

                request.SupplierName = supplier?.Name;
                request.SupplierNumber = supplierNumber;

                var (errorList, parentReferenceNo) = await helperService.GetErrorListAsync(supplier, site, application, applicationForm, remainingAmount, accountCodingId);
                request.ErrorList = errorList;
                request.ParentReferenceNo = parentReferenceNo;

                if (request.ErrorList != null && request.ErrorList.Count > 0)
                {
                    request.DisableFields = true;
                }

                ApplicationPaymentRequestForm!.Add(request);
            }

            ApplicationPaymentRequestForm = helperService.SortByHierarchy(ApplicationPaymentRequestForm);

            // Populate parent-child validation data for frontend
            await helperService.PopulateParentChildValidationDataAsync(ApplicationPaymentRequestForm);

            var batchName = await paymentRequestAppService.GetNextBatchInfoAsync();
            BatchNumberDisplay = batchName;
            TotalAmount = ApplicationPaymentRequestForm?.Sum(x => x.Amount) ?? 0m;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ApplicationPaymentRequestForm == null) return NoContent();

            // Validate standalone and parent-child payment amounts against current DB state
            var standaloneErrors = await helperService.ValidateStandalonePaymentAmountsAsync(ApplicationPaymentRequestForm);
            if (standaloneErrors.Count != 0)
            {
                throw new UserFriendlyException(string.Join(" ", standaloneErrors));
            }

            var validationErrors = await helperService.ValidateParentChildPaymentAmountsAsync(ApplicationPaymentRequestForm);
            if (validationErrors.Count != 0)
            {
                throw new UserFriendlyException(string.Join(" ", validationErrors));
            }

            if (ApplicationPaymentRequestForm.Exists(payment => string.IsNullOrWhiteSpace(payment.SupplierNumber)))
            {
                throw new UserFriendlyException(
                    "Cannot submit payment request: Supplier number is missing for one or more applications.");
            }

            if (ApplicationPaymentRequestForm.Exists(payment => payment.SiteId == Guid.Empty))
            {
                throw new UserFriendlyException(
                    "Cannot submit payment request: Site is missing for one or more applications.");
            }

            // Resolve override once — used for both validation and mapping below
            bool hasOverridePermission = await AuthorizationService.IsGrantedAsync(PaymentsPermissions.Payments.AccountCodingOverride);
            Guid? accountCodingOverrideId = null;
            if (hasOverridePermission
                && !string.IsNullOrWhiteSpace(AccountCodingOverride)
                && Guid.TryParse(AccountCodingOverride, out var overrideGuid)
                && overrideGuid != Guid.Empty)
            {
                accountCodingOverrideId = overrideGuid;
            }

            if (accountCodingOverrideId == null
                && ApplicationPaymentRequestForm.Exists(payment => payment.AccountCodingId == null || payment.AccountCodingId == Guid.Empty))
            {
                throw new UserFriendlyException(
                    "Cannot submit payment request: Account Coding is missing for one or more applications.");
            }

            var payments = MapPaymentRequests(accountCodingOverrideId);

            await paymentRequestAppService.CreateAsync(payments);

            return NoContent();
        }

        private List<CreatePaymentRequestDto> MapPaymentRequests(Guid? accountCodingOverrideId)
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
                    AccountCodingId = accountCodingOverrideId ?? payment.AccountCodingId
                });
            }

            return payments;
        }
    }
}
