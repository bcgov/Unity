using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Unity.GrantManager.GrantApplications;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Unity.GrantManager.Locality;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo
{

    [Widget(
        RefreshUrl = "Widget/SupplierInfo/Refresh",
        AutoInitialize = true)]
    public class SupplierInfoViewComponent : AbpViewComponent
    {

        public SupplierInfoViewComponent()
        {
           
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {

            SupplierInfoViewModel model = new()
            {
                SupplierNumber = "12345"
            };

            return View("~/Views/Shared/Components/SupplierInfo/Default.cshtml", model);
            
        }
    }

    
}
