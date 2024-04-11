using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Unity.Payments.Suppliers;
using Unity.GrantManager.Payments;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo
{
    [Widget(
        RefreshUrl = "Widget/SupplierInfo/Refresh",
        ScriptTypes = new[] { typeof(SupplierInfoWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(SupplierInfosWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class SupplierInfoViewComponent : AbpViewComponent
    {
        private readonly SupplierAppService _supplierService;
        public SupplierInfoViewComponent(SupplierAppService supplierService)
        {
            _supplierService = supplierService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            var supplier = await _supplierService.GetByCorrelationAsync(new GetSupplierByCorrelationDto()
            {
                CorrelationId = applicantId,
                CorrelationProvider = PaymentConsts.ApplicantCorrelationProvider
            });

            return View(new SupplierInfoViewModel()
            {
                SupplierCorrelationId = applicantId,
                SupplierCorrelationProvider = PaymentConsts.ApplicantCorrelationProvider,
                SupplierId = supplier?.Id ?? Guid.Empty,
                SupplierNumber = supplier?.Number?.ToString()
            });
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
