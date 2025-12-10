using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.Enums;
using Unity.Payments.PaymentRequests;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Features;

namespace Unity.Payments.Web.Views.Shared.Components.PaymentInfo
{
    [Widget(
       RefreshUrl = "Widget/PaymentInfo/Refresh",
       ScriptTypes = [typeof(PaymentInfoScriptBundleContributor)],
       StyleTypes = [typeof(PaymentInfoStyleBundleContributor)],
       AutoInitialize = true)]
    public class PaymentInfoViewComponent : AbpViewComponent
    {
        private readonly GrantApplicationAppService _grantApplicationAppService;
        private readonly IPaymentRequestAppService _paymentRequestService;
        private readonly IFeatureChecker _featureChecker;
        private readonly IApplicationLinksService _applicationLinksService;

        public PaymentInfoViewComponent(GrantApplicationAppService grantApplicationAppService,
                 IPaymentRequestAppService paymentRequestService,
                 IFeatureChecker featureChecker,
                 IApplicationLinksService applicationLinksService)
        {
            _grantApplicationAppService = grantApplicationAppService;
            _paymentRequestService = paymentRequestService;
            _featureChecker = featureChecker;
            _applicationLinksService = applicationLinksService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId, Guid applicationFormVersionId)
        {
            if (await _featureChecker.IsEnabledAsync("Unity.Payments"))
            {
                var application = await _grantApplicationAppService.GetAsync(applicationId);
                PaymentInfoViewModel model = new()
                {
                    RequestedAmount = application.RequestedAmount,
                    RecommendedAmount = application.RecommendedAmount,
                    ApprovedAmount = application.ApprovedAmount,
                    ApplicationId = applicationId,
                    ApplicationFormVersionId = applicationFormVersionId,
                    ApplicantId = application.Applicant.Id
                };
                
                var paymentRequests = await _paymentRequestService.GetListByApplicationIdAsync(applicationId);

                // Calculate Total Paid for current application
                model.TotalPaid = paymentRequests
                    .Where(e => e.Status == PaymentRequestStatus.Submitted)
                    .Sum(e => e.Amount);

                // Calculate Total Pending Amounts for current application
                model.TotalPendingAmounts = paymentRequests
                    .Where(e => e.Status is not (PaymentRequestStatus.Paid
                                                or PaymentRequestStatus.L1Declined
                                                or PaymentRequestStatus.L2Declined
                                                or PaymentRequestStatus.L3Declined))
                    .Sum(e => e.Amount);

                // Add Total Paid and Total Pending Amounts from child applications
                var applicationLinks = await _applicationLinksService.GetListByApplicationAsync(applicationId);
                var childApplications = applicationLinks
                    .Where(link => link.LinkType == ApplicationLinkType.Child
                                && link.ApplicationId != applicationId)  // Exclude self-references
                    .ToList();

                // Batch fetch payment requests for all child applications to avoid N+1 queries
                var childApplicationIds = childApplications.Select(ca => ca.ApplicationId).ToList();
                if (childApplicationIds.Any())
                {
                    var childPaymentRequests = await _paymentRequestService.GetListByApplicationIdsAsync(childApplicationIds);
                    var paymentRequestsByAppId = childPaymentRequests
                        .GroupBy(pr => pr.ApplicationId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var childApp in childApplications)
                    {
                        if (paymentRequestsByAppId.TryGetValue(childApp.ApplicationId, out var requests))
                        {
                            // Add child's Total Paid
                            var childPaidAmount = requests
                                .Where(e => e.Status == PaymentRequestStatus.Submitted)
                                .Sum(e => e.Amount);
                            model.TotalPaid += childPaidAmount;

                            // Add child's Total Pending Amounts
                            var childPendingAmount = requests
                                .Where(e => e.Status is not (PaymentRequestStatus.Paid
                                                            or PaymentRequestStatus.L1Declined
                                                            or PaymentRequestStatus.L2Declined
                                                            or PaymentRequestStatus.L3Declined))
                                .Sum(e => e.Amount);
                            model.TotalPendingAmounts += childPendingAmount;
                        }
                    }
                }

                model.RemainingAmount = application.ApprovedAmount - model.TotalPaid;

                return View(model);
            }
            else
                return View(new PaymentInfoViewModel());
        }

        public class PaymentInfoStyleBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/PaymentInfo/Default.css");
            }
        }

        public class PaymentInfoScriptBundleContributor : BundleContributor
        {
            public override void ConfigureBundle(BundleConfigurationContext context)
            {
                context.Files
                  .AddIfNotContains("/Views/Shared/Components/PaymentInfo/Default.js");
                context.Files
                  .AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
            }
        }
    }
}
