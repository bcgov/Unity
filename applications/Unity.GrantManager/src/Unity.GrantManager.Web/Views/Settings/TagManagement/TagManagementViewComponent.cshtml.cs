using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Settings.TagManagement;

[Widget(
    ScriptTypes = new[] { typeof(TagManagementScriptBundleContributor) },
    AutoInitialize = true
)]
public class TagManagementViewComponent : AbpViewComponent
{
    public virtual IViewComponentResult Invoke()
    {
        return View("~/Views/Settings/TagManagement/TagManagementViewComponent.cshtml");
    }
}

public class TagManagementScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/Views/Settings/TagManagement/TagManagement.js");
    }
}
