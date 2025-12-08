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
using Unity.GrantManager.ApplicationForms;

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
        IApplicationFormVersionRepository applicationFormVersionRepository,
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
            model.FormHierarchy = applicationForm?.FormHierarchy;
            model.ParentFormId = applicationForm?.ParentFormId;
            model.ParentFormVersionId = applicationForm?.ParentFormVersionId;

            // Load parent form display name if parent form is selected
            if (model.ParentFormId.HasValue && model.ParentFormVersionId.HasValue)
            {
                var parentForm = await applicationFormRepository.FindAsync(model.ParentFormId.Value);
                var parentFormVersion = await applicationFormVersionRepository.FindAsync(model.ParentFormVersionId.Value);

                if (parentForm != null && parentFormVersion != null)
                {
                    model.ParentFormDisplayName = $"{parentForm.ApplicationFormName} V{parentFormVersion.Version}.0";
                }
            }

            model.FormHierarchyList = new()
            {
                new SelectListItem { Value = ((int)FormHierarchyType.Parent).ToString(), Text = "Parent Form" },
                new SelectListItem { Value = ((int)FormHierarchyType.Child).ToString(), Text = "Child Form" }
            };
            model.AccountCodeList = new();
            foreach (var accountCoding in accountCodings)
            {
                string accountCodingText = await paymentConfigurationAppService.GetAccountDistributionCodeDescription(accountCoding);
                SelectListItem selectListItem = new() { Value = accountCoding.Id.ToString(), Text = accountCodingText };
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
            context.Files
              .AddIfNotContains("/libs/select2/css/select2.min.css");
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
            context.Files
              .AddIfNotContains("/libs/select2/js/select2.full.min.js");
        }
    }
}
