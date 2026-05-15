using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Suppliers;

namespace Unity.Payments.Web.Pages.Payments
{
    public class PaymentRequestPageHelperService(
        IGrantApplicationAppService applicationService,
        IPaymentRequestAppService paymentRequestAppService,
        IApplicationLinksService applicationLinksService,
        ISupplierAppService supplierAppService,
        IApplicationFormAppService applicationFormAppService
    )
    {
        public async Task<decimal> GetRemainingAmountAsync(GrantApplicationDto application)
        {
            decimal remainingAmount = 0;
            if (application.ApprovedAmount > 0)
            {
                decimal approvedAmount = application.ApprovedAmount;
                decimal totalFutureRequested = await paymentRequestAppService.GetTotalPaymentRequestAmountByCorrelationIdAsync(application.Id);

                decimal childrenTotalPaidPending = 0;
                var applicationLinks = await applicationLinksService.GetListByApplicationAsync(application.Id);
                var childLinks = applicationLinks
                    .Where(link => link.LinkType == ApplicationLinkType.Child && link.ApplicationId != application.Id)
                    .ToList();

                foreach (var childLink in childLinks)
                {
                    decimal childTotal = await paymentRequestAppService
                        .GetTotalPaymentRequestAmountByCorrelationIdAsync(childLink.ApplicationId);
                    childrenTotalPaidPending += childTotal;
                }

                decimal totalConsumed = totalFutureRequested + childrenTotalPaidPending;
                if (approvedAmount > totalConsumed)
                {
                    remainingAmount = approvedAmount - totalConsumed;
                }
            }

            return remainingAmount;
        }

        public async Task<SupplierDto?> GetSupplierAsync(GrantApplicationDto application)
        {
            if (application.Applicant.SupplierId != Guid.Empty)
            {
                return await supplierAppService.GetAsync(application.Applicant.SupplierId);
            }

            return null;
        }

        public async Task<(List<string> Errors, string? ParentReferenceNo)> ValidateFormHierarchyAndParentLink(
            GrantApplicationDto application,
            ApplicationForm applicationForm)
        {
            List<string> errors = [];
            string? parentReferenceNo = null;

            if (!applicationForm.Payable ||
                !applicationForm.FormHierarchy.HasValue ||
                applicationForm.FormHierarchy.Value != FormHierarchyType.Child)
            {
                return (errors, parentReferenceNo);
            }

            if (!applicationForm.ParentFormId.HasValue)
            {
                return (errors, parentReferenceNo);
            }

            var allLinks = await applicationLinksService.GetListByApplicationAsync(application.Id);
            var parentLink = allLinks.Find(link => link.LinkType == ApplicationLinkType.Parent && link.ApplicationId != application.Id);

            if (parentLink == null)
            {
                errors.Add("Payment Configuration for this form requires a valid parent application link before payments can be processed.");
                return (errors, parentReferenceNo);
            }

            var parentFormDetails = await applicationFormAppService.GetFormDetailsByApplicationIdAsync(parentLink.ApplicationId);
            var parentApplication = await applicationService.GetAsync(parentLink.ApplicationId);
            parentReferenceNo = parentApplication.ReferenceNo;

            bool formIdMatches = parentFormDetails.ApplicationFormId == applicationForm.ParentFormId.Value;
            if (!formIdMatches)
            {
                errors.Add("The selected parent form in Payment Configuration does not match the application's linked parent. Please verify and try again.");
                return (errors, parentReferenceNo);
            }

            return (errors, parentReferenceNo);
        }

        public async Task<(List<string> ErrorList, string? ParentReferenceNo)> GetErrorListAsync(
            SupplierDto? supplier,
            Site? site,
            GrantApplicationDto application,
            ApplicationForm applicationForm,
            decimal remainingAmount,
            Guid? accountCodingId,
            bool isHistorical = false)
        {
            bool missingFields = false;
            List<string> errorList = [];

            if (!isHistorical && (supplier == null || site == null || string.IsNullOrWhiteSpace(supplier.Number)))
            {
                missingFields = true;
            }

            if (!isHistorical && site != null && site.PaymentGroup == PaymentGroup.EFT && string.IsNullOrWhiteSpace(site.BankAccount))
            {
                errorList.Add("Payment cannot be submitted because the default site's pay group is set to EFT, but no bank account is configured. Please update the bank account before proceeding.");
            }

            if (remainingAmount <= 0)
            {
                errorList.Add("There is no remaining amount for this application.");
            }

            if (missingFields)
            {
                errorList.Add("Some payment information is missing for this applicant.  Please make sure supplier information is provided and default site is selected.");
            }

            if (application.StatusCode != GrantApplicationState.GRANT_APPROVED)
            {
                errorList.Add("The selected Application is not Approved. To continue please remove the item from the list.");
            }

            var allLinks = await applicationLinksService.GetListByApplicationAsync(application.Id);
            var parentLink = allLinks.Find(link => link.LinkType == ApplicationLinkType.Parent && link.ApplicationId != application.Id);
            if (parentLink != null)
            {
                var parentApplication = await applicationService.GetAsync(parentLink.ApplicationId);
                if (parentApplication.Id == Guid.Empty || parentApplication.StatusCode != GrantApplicationState.GRANT_APPROVED)
                {
                    errorList.Add("Payment cannot be processed because the linked parent submission is not approved. Please ensure the parent submission is approved before creating a payment.");
                }
            }

            if (!application.ApplicationForm.Payable)
            {
                errorList.Add("The selected application is not Payable. To continue please remove the item from the list.");
            }

            if (!isHistorical && (accountCodingId == null || accountCodingId == Guid.Empty))
            {
                errorList.Add("The selected application form does not have an Account Coding or no default Account Coding is set.");
            }

            var (hierarchyErrors, parentReferenceNo) = await ValidateFormHierarchyAndParentLink(application, applicationForm);
            errorList.AddRange(hierarchyErrors);

            return (errorList, parentReferenceNo);
        }

        public List<T> SortByHierarchy<T>(List<T> paymentRequests) where T : IPaymentFormItem
        {
            var sortedList = new List<T>();
            var processed = new HashSet<string>();

            var parentGroups = paymentRequests
                .Where(x => !string.IsNullOrEmpty(x.ParentReferenceNo))
                .GroupBy(x => x.ParentReferenceNo)
                .ToList();

            foreach (var group in parentGroups.OrderBy(g => g.Key))
            {
                string parentRefNo = group.Key!;

                var children = group.OrderBy(x => x.InvoiceNumber).ToList();
                sortedList.AddRange(children);
                foreach (var child in children)
                {
                    processed.Add(child.InvoiceNumber);
                }

                var parent = paymentRequests
                    .Find(x => x.SubmissionConfirmationCode == parentRefNo && string.IsNullOrEmpty(x.ParentReferenceNo));

                if (parent != null)
                {
                    sortedList.Add(parent);
                    processed.Add(parent.InvoiceNumber);
                }
            }

            var standaloneItems = paymentRequests
                .Where(x => !processed.Contains(x.InvoiceNumber))
                .OrderBy(x => x.InvoiceNumber)
                .ToList();
            sortedList.AddRange(standaloneItems);

            return sortedList;
        }

        public async Task PopulateParentChildValidationDataAsync<T>(List<T> form) where T : IPaymentFormItem
        {
            var childGroups = form
                .Where(x => !string.IsNullOrEmpty(x.ParentReferenceNo))
                .GroupBy(x => x.ParentReferenceNo);

            foreach (var childGroup in childGroups)
            {
                string parentRefNo = childGroup.Key!;
                var children = childGroup.ToList();

                var parentInSubmission = form
                    .Find(x => x.SubmissionConfirmationCode == parentRefNo && string.IsNullOrEmpty(x.ParentReferenceNo));

                Guid parentApplicationId;

                if (parentInSubmission != null)
                {
                    parentApplicationId = parentInSubmission.CorrelationId;
                }
                else
                {
                    var firstChild = await applicationService.GetAsync(children[0].CorrelationId);
                    var allLinks = await applicationLinksService.GetListByApplicationAsync(firstChild.Id);
                    var parentLink = allLinks.Find(link => link.LinkType == ApplicationLinkType.Parent && link.ApplicationId != firstChild.Id);

                    if (parentLink == null) continue;

                    parentApplicationId = parentLink.ApplicationId;
                }

                var parentApplication = await applicationService.GetAsync(parentApplicationId);
                decimal approvedAmount = parentApplication.ApprovedAmount;

                decimal parentTotalPaidPending = await paymentRequestAppService
                    .GetTotalPaymentRequestAmountByCorrelationIdAsync(parentApplicationId);

                var parentLinks = await applicationLinksService.GetListByApplicationAsync(parentApplicationId);
                var allChildLinks = parentLinks.Where(link => link.LinkType == ApplicationLinkType.Child).ToList();

                decimal childrenTotalPaidPending = 0;
                foreach (var childLink in allChildLinks)
                {
                    decimal childTotal = await paymentRequestAppService
                        .GetTotalPaymentRequestAmountByCorrelationIdAsync(childLink.ApplicationId);
                    childrenTotalPaidPending += childTotal;
                }

                decimal maximumPaymentAmount = approvedAmount - (parentTotalPaidPending + childrenTotalPaidPending);

                foreach (var child in children)
                {
                    child.MaximumAllowedAmount = maximumPaymentAmount;
                    child.IsPartOfParentChildGroup = true;
                    child.ParentApprovedAmount = approvedAmount;
                }

                if (parentInSubmission != null)
                {
                    parentInSubmission.MaximumAllowedAmount = maximumPaymentAmount;
                    parentInSubmission.IsPartOfParentChildGroup = true;
                    parentInSubmission.ParentApprovedAmount = approvedAmount;
                }
            }
        }

        public async Task<List<string>> ValidateParentChildPaymentAmountsAsync<T>(List<T> form) where T : IPaymentFormItem
        {
            List<string> errors = [];

            var childGroups = form
                .Where(x => !string.IsNullOrEmpty(x.ParentReferenceNo))
                .GroupBy(x => x.ParentReferenceNo);

            foreach (var childGroup in childGroups)
            {
                string parentRefNo = childGroup.Key!;
                var children = childGroup.ToList();

                var parentInSubmission = form
                    .Find(x => x.SubmissionConfirmationCode == parentRefNo && string.IsNullOrEmpty(x.ParentReferenceNo));

                Guid parentApplicationId;
                decimal currentParentAmount = 0;

                if (parentInSubmission != null)
                {
                    parentApplicationId = parentInSubmission.CorrelationId;
                    currentParentAmount = parentInSubmission.Amount;
                }
                else
                {
                    var firstChild = await applicationService.GetAsync(children[0].CorrelationId);
                    var allLinks = await applicationLinksService.GetListByApplicationAsync(firstChild.Id);
                    var parentLink = allLinks.Find(link => link.LinkType == ApplicationLinkType.Parent && link.ApplicationId != firstChild.Id);

                    if (parentLink == null)
                    {
                        errors.Add($"Parent application link not found for reference {parentRefNo}.");
                        continue;
                    }
                    parentApplicationId = parentLink.ApplicationId;
                }

                var parentApplication = await applicationService.GetAsync(parentApplicationId);
                decimal approvedAmount = parentApplication.ApprovedAmount;

                decimal parentTotalPaidPending = await paymentRequestAppService
                    .GetTotalPaymentRequestAmountByCorrelationIdAsync(parentApplicationId);

                var parentLinks = await applicationLinksService.GetListByApplicationAsync(parentApplicationId);
                var allChildLinks = parentLinks.Where(link => link.LinkType == ApplicationLinkType.Child).ToList();

                decimal childrenTotalPaidPending = 0;
                foreach (var childLink in allChildLinks)
                {
                    decimal childTotal = await paymentRequestAppService
                        .GetTotalPaymentRequestAmountByCorrelationIdAsync(childLink.ApplicationId);
                    childrenTotalPaidPending += childTotal;
                }

                decimal maximumPaymentAmount = approvedAmount - (parentTotalPaidPending + childrenTotalPaidPending);

                decimal currentChildrenAmount = children.Sum(x => x.Amount);
                decimal currentSubmissionTotal = currentParentAmount + currentChildrenAmount;

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
