using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;


namespace Unity.GrantManager.Web.Views.Shared.Components.UserInfoWidget
{
    [Widget(
        RefreshUrl = "Widgets/UserInfo/RefreshUserInfo",
        ScriptTypes = new[] { typeof(UserInfoWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(UserInfoWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class UserInfoWidgetViewComponent : AbpViewComponent
    {
       

        public UserInfoWidgetViewComponent()
        {
          
        }

        public async Task<IViewComponentResult> InvokeAsync(string name, string info)
        {            
            UserInfoWidgetViewModel model = new()
            {
               Name = name,
               Info = info,
            };

            return View(model);            
        }

    }

    public class UserInfoWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/UserInfoWidget/Default.css");
        }
    }

    public class UserInfoWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/UserInfoWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }

}
