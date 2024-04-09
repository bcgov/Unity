using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Unity.Payments.Suppliers;
using System.Linq;
using System.Linq.Dynamic.Core;
using Volo.Abp.Application.Services;
using Unity.Payments.SupplierInfo;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo
{
    [Widget(
        RefreshUrl = "Widget/SupplierInfo/Refresh",
        ScriptTypes = new[] { typeof(SupplierInfoWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(SupplierInfosWidgetStyleBundleContributor) },        
        AutoInitialize = true)]
    public class SupplierInfoViewComponent : AbpViewComponent
    {
        private readonly SupplierInfoAppService _supplierService;
        public SupplierInfoViewComponent(SupplierInfoAppService supplierService)
        {
            _supplierService = supplierService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            Task<Supplier?> supplier = _supplierService.GetSupplierAsync(applicantId);
            return View(new SupplierInfoViewModel() { SupplierNumber = supplier.Result?.Number.ToString()});
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
