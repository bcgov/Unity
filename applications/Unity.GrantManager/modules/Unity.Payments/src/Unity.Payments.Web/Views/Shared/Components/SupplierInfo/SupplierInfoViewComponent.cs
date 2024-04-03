using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo
{
    [Widget(
        RefreshUrl = "Widget/SupplierInfo/Refresh",
        ScriptTypes = new[] { typeof(SupplierInfoWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(SupplierInfosWidgetStyleBundleContributor) },        
        AutoInitialize = true)]
    public class SupplierInfoViewComponent : AbpViewComponent
    {

        public SupplierInfoViewComponent()
        {
           
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            await Task.CompletedTask; // remove

            SupplierInfoViewModel model = new()
            {
                SupplierNumber = "12345"
            };

            return View(model);
            
        }
    }

    public class SupplierInfosWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/SupplierInfo/SupplierInfo.css");
        }
    }

    public class SupplierInfoWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/SupplierInfo/SupplierInfo.js");
        }
    }
}
