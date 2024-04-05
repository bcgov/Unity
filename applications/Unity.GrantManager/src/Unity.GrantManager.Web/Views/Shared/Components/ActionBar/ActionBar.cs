using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Unity.GrantManager.Web.Views.Shared.Components.ActionBar
{
    [Widget(ScriptFiles = new[] { 
        "/Views/Shared/Components/ActionBar/Default.js", 
        "/Pages/ApplicationTags/ApplicationTags.js", 
        "/Pages/AssigneeSelection/AssigneeSelection.js",
        "/Pages/BatchPayments/BatchPaymentsModal.js",
        "/libs/jquery-maskmoney/dist/jquery.maskMoney.min.js",
    },
        StyleFiles = new[] { "/Views/Shared/Components/ActionBar/Default.css" })]
      public class ActionBar : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}

