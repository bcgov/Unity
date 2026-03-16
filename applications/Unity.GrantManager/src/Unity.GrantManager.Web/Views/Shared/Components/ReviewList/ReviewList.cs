using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.ReviewList
{
    [Widget(
        ScriptFiles = new[]
        {
            "/Views/Shared/Components/ReviewList/ReviewList.js"
        },
        StyleFiles = new[]
        {
            "/Views/Shared/Components/ReviewList/ReviewList.css"
        })]
    public class ReviewList : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
