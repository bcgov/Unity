using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.PaymentRequests;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Payments;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.PaymentConfigurations;

namespace Unity.Payments.Web.Pages.Payments
{
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Primary constructor for dependency injection")]
    public class CreateHistoricalPaymentsModel(
        IGrantApplicationAppService applicationService,
        IPaymentRequestAppService paymentRequestAppService,
        IApplicationFormRepository applicationFormRepository,
        ISiteRepository siteRepository,
        IPaymentSettingsAppService paymentSettingsAppService,
        ApplicationIdsCacheService cacheService,
        PaymentRequestPageHelperService helperService
    ) : AbpPageModel
    {
        public List<Guid> SelectedApplicationIds { get; set; } = [];

        [BindProperty]
        public List<HistoricalPaymentsModel> ApplicationPaymentRequestForm { get; set; } = [];

        [BindProperty]
        public bool DisableSubmit { get; set; }

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
                Logger.LogInformation("Successfully loaded historical payment modal for {Count} applications", applicationIds.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading historical payment modal");
                ViewData["Error"] = "An error occurred while loading the historical payment form. Please try again.";
                DisableSubmit = true;
                return;
            }

            var applications = await applicationService.GetApplicationDetailsListAsync(SelectedApplicationIds);

            foreach (var application in applications)
            {
                decimal remainingAmount = await helperService.GetRemainingAmountAsync(application);
                var applicationForm = await applicationFormRepository.GetAsync(application.ApplicationForm.Id);
                Guid? accountCodingId = await paymentSettingsAppService.GetAccountCodingIdByApplicationIdAsync(application.Id);

                HistoricalPaymentsModel request = new()
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

                Guid? siteId = application.DefaultSiteId;
                Site? site = null;
                if (siteId.HasValue && siteId != Guid.Empty)
                {
                    site = await siteRepository.GetAsync(siteId.Value);
                    request.SiteName = $"{site.Number} ({supplierNumber}, {site.City})";
                    request.SiteId = siteId;
                }

                request.SupplierName = supplier?.Name;
                request.SupplierNumber = supplierNumber;

                var (errorList, parentReferenceNo) = await helperService.GetErrorListAsync(
                    supplier, site, application, applicationForm, remainingAmount,
                    accountCodingId, isHistorical: true);

                request.ErrorList = errorList;
                request.ParentReferenceNo = parentReferenceNo;

                if (request.ErrorList.Count > 0)
                {
                    request.DisableFields = true;
                }

                ApplicationPaymentRequestForm!.Add(request);
            }

            ApplicationPaymentRequestForm = helperService.SortByHierarchy(ApplicationPaymentRequestForm);
            await helperService.PopulateParentChildValidationDataAsync(ApplicationPaymentRequestForm);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ApplicationPaymentRequestForm == null) return NoContent();

            foreach (var payment in ApplicationPaymentRequestForm)
            {
                if (string.IsNullOrWhiteSpace(payment.PaidDate))
                    throw new UserFriendlyException("Paid Date is required for all historical payments.");

                if (!DateTime.TryParseExact(payment.PaidDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var paidDate))
                    throw new UserFriendlyException($"Paid Date '{payment.PaidDate}' is not a valid date.");

                if (paidDate.Date > DateTime.Today)
                    throw new UserFriendlyException("Paid Date cannot be in the future.");

                payment.PaidDate = paidDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

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

            var payments = ApplicationPaymentRequestForm.Select(payment => new CreateHistoricalPaymentRequestDto()
            {
                Amount = payment.Amount,
                CorrelationId = payment.CorrelationId,
                SiteId = payment.SiteId,
                AccountCodingId = payment.AccountCodingId,
                Description = payment.Description,
                InvoiceNumber = payment.InvoiceNumber,
                ContractNumber = payment.ContractNumber ?? string.Empty,
                SupplierName = payment.SupplierName,
                SupplierNumber = payment.SupplierNumber,
                PayeeName = payment.ApplicantName ?? string.Empty,
                SubmissionConfirmationCode = payment.SubmissionConfirmationCode ?? string.Empty,
                CorrelationProvider = PaymentConsts.ApplicationCorrelationProvider,
                PaidDate = payment.PaidDate,
            }).ToList();

            await paymentRequestAppService.CreateHistoricalAsync(payments);

            return NoContent();
        }
    }
}
