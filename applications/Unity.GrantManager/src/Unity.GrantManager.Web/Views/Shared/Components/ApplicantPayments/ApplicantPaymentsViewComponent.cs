using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.Payments;
using Unity.Modules.Shared;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantPayments;

[Widget(
    RefreshUrl = "Widget/ApplicantPayments/Refresh",
    ScriptTypes = [typeof(ApplicantPaymentsScriptBundleContributor)],
    StyleTypes = [typeof(ApplicantPaymentsStyleBundleContributor)],
    AutoInitialize = true)]
public class ApplicantPaymentsViewComponent(
    IApplicantPaymentsAppService applicantPaymentsAppService,
    IFeatureChecker featureChecker,
    IPermissionChecker permissionChecker) : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
    {
        var emptyModel = new ApplicantPaymentsViewModel { ApplicantId = applicantId };

        if (!await featureChecker.IsEnabledAsync(PaymentConsts.UnityPaymentsFeature))
            return View(emptyModel);

        if (!await permissionChecker.IsGrantedAsync(UnitySelector.Payment.Summary.Default))
            return View(emptyModel);

        var summary = await applicantPaymentsAppService.GetPaymentSummaryByApplicantIdAsync(applicantId);

        return View(new ApplicantPaymentsViewModel
        {
            ApplicantId = applicantId,
            TotalApprovedAmount = summary.TotalApprovedAmount,
            TotalPaidAmount = summary.TotalPaidAmount,
            TotalRemainingAmount = summary.TotalRemainingAmount
        });
    }
}

public class ApplicantPaymentsScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/Views/Shared/Components/ApplicantPayments/Default.js");
        context.Files.AddIfNotContains("/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js");
    }
}

public class ApplicantPaymentsStyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/Views/Shared/Components/ApplicantPayments/Default.css");
    }
}
