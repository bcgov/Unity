using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.DetailsActionBar;

[Widget(ScriptFiles = new[] { "/Views/Shared/Components/DetailsActionBar/Default.js" },
    StyleFiles = new[] { "/Views/Shared/Components/ActionBar/Default.css" })]
  public class DetailsActionBar : AbpViewComponent
{
    [TempData]
    public string SelectedApplicationId { get; set; } = "";
    public IViewComponentResult Invoke(string ApplicationId)
    {
        SelectedApplicationId = ApplicationId;
        return View();
    }
}

