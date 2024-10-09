using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Unity.GrantManager.GrantApplications;
using Unity.Payments.PaymentRequests;
using System.Linq;
using Unity.Payments.Enums;
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

        public PaymentInfoViewComponent(GrantApplicationAppService grantApplicationAppService,
                 IPaymentRequestAppService paymentRequestService,
                 IFeatureChecker featureChecker)
        {
            _grantApplicationAppService = grantApplicationAppService;
            _paymentRequestService = paymentRequestService;
            _featureChecker = featureChecker;
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
                    ApplicationFormVersionId = applicationFormVersionId
                };
                var paymentRequests = await _paymentRequestService.GetListByApplicationIdAsync(applicationId);

           model.TotalPaid= paymentRequests.Where(e => e.Status.Equals(PaymentRequestStatus.Paid))
                                  .Sum(e => e.Amount);
            model.TotalPendingAmounts = paymentRequests.Where(e => e.Status != PaymentRequestStatus.Paid).Sum(e => e.Amount);
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
