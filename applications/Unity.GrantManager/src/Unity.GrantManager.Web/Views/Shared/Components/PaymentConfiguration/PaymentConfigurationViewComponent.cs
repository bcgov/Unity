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
using Unity.Payments.Enums;

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
            model.FormHierarchy = applicationForm?.FormHierarchy;
            model.ParentFormId = applicationForm?.ParentFormId;
            model.DefaultPaymentGroup = applicationForm?.DefaultPaymentGroup ?? (int)PaymentGroup.EFT;

            // Load parent form display name if parent form is selected
            if (model.ParentFormId.HasValue)
            {
                var parentForm = await applicationFormRepository.FindAsync(model.ParentFormId.Value);

                if (parentForm != null)
                {
                    var formName = parentForm.ApplicationFormName ?? string.Empty;
                    var category = parentForm.Category;
                    model.ParentFormDisplayName = !string.IsNullOrEmpty(category)
                        ? $"{formName} - {category}"
                        : formName;
                }
            }

            model.FormHierarchyList = new()
            {
                new SelectListItem { Value = ((int)FormHierarchyType.Parent).ToString(), Text = "Parent Form" },
                new SelectListItem { Value = ((int)FormHierarchyType.Child).ToString(), Text = "Child Form" }
            };
            model.PaymentGroupList = new()
            {
                new SelectListItem { Value = ((int)PaymentGroup.EFT).ToString(), Text = "EFT" },
                new SelectListItem { Value = ((int)PaymentGroup.Cheque).ToString(), Text = "Cheque" }
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
