using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.Notifications.Web.Bundling
{
    public class NotificationsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {            
            context.Files.AddIfNotContains("/libs/tinymce/skins/ui/oxide/content.css");            
            context.Files.AddIfNotContains("/libs/tinymce/skins/ui/oxide/skin.css");
        }
    }
}
