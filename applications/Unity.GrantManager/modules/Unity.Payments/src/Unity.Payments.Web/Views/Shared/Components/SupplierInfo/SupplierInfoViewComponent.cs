using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Unity.Payments.Suppliers;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Features;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo
{
    [Widget(
        RefreshUrl = "Widget/SupplierInfo/Refresh",
        ScriptTypes = [typeof(SupplierInfoWidgetScriptBundleContributor)],
        StyleTypes = [typeof(SupplierInfosWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class SupplierInfoViewComponent : AbpViewComponent
    {
        private readonly ISupplierAppService _supplierService;
        private readonly IFeatureChecker _featureChecker;

        public SupplierInfoViewComponent(ISupplierAppService supplierService,
            IFeatureChecker featureChecker)
        {
            _supplierService = supplierService;
            _featureChecker = featureChecker;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicantId)
        {
            if (await _featureChecker.IsEnabledAsync("Unity.Payments"))
            {
                var supplier = await _supplierService.GetByCorrelationAsync(new GetSupplierByCorrelationDto()
                {
                    CorrelationId = applicantId,
                    CorrelationProvider = CorrelationConsts.Applicant
                });

                return View(new SupplierInfoViewModel()
                {
                    SupplierCorrelationId = applicantId,
                    SupplierCorrelationProvider = CorrelationConsts.Applicant,
                    SupplierId = supplier?.Id ?? Guid.Empty, 
                    SupplierNumber = supplier?.Number?.ToString(),
                    SupplierName = supplier?.Name?.ToString(),
                    Status = supplier?.Status?.ToString(),
                    OriginalSupplierNumber = supplier?.Number?.ToString(),
                });
            } else
            {
                return View(new SupplierInfoViewModel());
            }
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
