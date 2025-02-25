using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.GrantManager.Web.Views.Settings.BackgroundJobsSettingGroup;

public class BackgroundJobsScriptBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.AddIfNotContains("/Views/Settings/BackgroundJobsGroup/Default.js");
    }
}
