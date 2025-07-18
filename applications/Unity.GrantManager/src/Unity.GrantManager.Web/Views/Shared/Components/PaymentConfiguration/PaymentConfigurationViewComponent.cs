using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Microsoft.AspNetCore.Mvc.Rendering;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.Permissions;
using Volo.Abp.Authorization.Permissions;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Web.Views.Shared.Components.PaymentConfiguration
{
    [Widget(
        RefreshUrl = "PaymentConfiguration/Refresh",
        ScriptTypes = new[] { typeof(PaymentConfigurationScriptBundleContributor) },
        StyleTypes = new[] { typeof(PaymentConfigurationStyleBundleContributor) },
        AutoInitialize = true)]
    public class PaymentConfigurationViewComponent(
        IAccountCodingRepository accountCodingRepository,
        IApplicationFormRepository applicationFormRepository, 
        IPermissionChecker permissionChecker,
        PaymentConfigurationAppService paymentConfigurationAppService) : AbpViewComponent
    {

        public string AccountCodeList { get; set; } = string.Empty;

        public async Task<IViewComponentResult> InvokeAsync(Guid formId)
        {         
            ApplicationForm? applicationForm = await applicationFormRepository.GetAsync(formId);   
            List<AccountCoding> accountCodings = await accountCodingRepository.GetListAsync();
            PaymentConfigurationViewModel model = new();
            model.HasEditFormPaymentConfiguration = await HasEditPaymentConfiguration();
            model.Payable = applicationForm?.Payable ?? false;
            model.PaymentApprovalThreshold = applicationForm?.PaymentApprovalThreshold;
            model.PreventAutomaticPaymentToCAS = applicationForm?.PreventPayment ?? false;
            model.AccountCode = applicationForm?.AccountCodingId;
            model.AccountCodeList = new();
            foreach (var accountCoding in accountCodings)
            {
                string accountCodingText = await paymentConfigurationAppService.GetAccountDistributionCode(accountCoding);
                SelectListItem selectListItem = new SelectListItem { Value = accountCoding.Id.ToString(), Text = accountCodingText};
                model.AccountCodeList.Add(selectListItem);
            }

            return View(model);
        }        

        private async Task<bool> HasEditPaymentConfiguration()
        {
            return await permissionChecker.IsGrantedAsync(PaymentsPermissions.Payments.EditFormPaymentConfiguration);
        }
    }

    public class PaymentConfigurationStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/PaymentConfiguration/Default.css");
        }
    }

    public class PaymentConfigurationScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/PaymentConfiguration/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
