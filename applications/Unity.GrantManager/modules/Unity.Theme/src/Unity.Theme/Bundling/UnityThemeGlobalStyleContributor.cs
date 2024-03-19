using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.AspNetCore.Mvc.UI.Themes.Bundling;

public class UnityThemeGlobalStyleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add("/themes/Unity/fluentui-icons.css");
        context.Files.Add("/themes/Unity/fluenticons.min.css");
        context.Files.Add("/themes/Unity/fonts.css");
        context.Files.Add("/themes/Unity/layout.css");
        context.Files.Add("/themes/Unity/unity-styles.css");

        context.Files.AddIfNotContains("/libs/datatables.net-bs5/css/dataTables.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons-bs5/css/buttons.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-select-bs5/css/select.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-colreorder-bs5/css/colReorder.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader-bs5/css/fixedHeader.bootstrap5.css");

        // Add assets for "/themes/Unity/fonts/**/*"
        context.Files.AddRange(new[] {
            "/themes/Unity/fonts/icons/Segoe-Fluent-Icons.ttf",
            "/themes/Unity/fonts/icons/Segoe-MDL2-Assets.ttf",
            "/themes/Unity/fonts/BCSans/BCSans-Bold.otf",
            "/themes/Unity/fonts/BCSans/BCSans-Bold.ttf",
            "/themes/Unity/fonts/BCSans/BCSans-Bold.woff",
            "/themes/Unity/fonts/BCSans/BCSans-Bold.woff2",
            "/themes/Unity/fonts/BCSans/BCSans-BoldItalic.otf",
            "/themes/Unity/fonts/BCSans/BCSans-BoldItalic.ttf",
            "/themes/Unity/fonts/BCSans/BCSans-BoldItalic.woff",
            "/themes/Unity/fonts/BCSans/BCSans-BoldItalic.woff2",
            "/themes/Unity/fonts/BCSans/BCSans-Italic.otf",
            "/themes/Unity/fonts/BCSans/BCSans-Italic.ttf",
            "/themes/Unity/fonts/BCSans/BCSans-Italic.woff",
            "/themes/Unity/fonts/BCSans/BCSans-Italic.woff2",
            "/themes/Unity/fonts/BCSans/BCSans-Light.otf",
            "/themes/Unity/fonts/BCSans/BCSans-Light.ttf",
            "/themes/Unity/fonts/BCSans/BCSans-Light.woff",
            "/themes/Unity/fonts/BCSans/BCSans-Light.woff2",
            "/themes/Unity/fonts/BCSans/BCSans-LightItalic.otf",
            "/themes/Unity/fonts/BCSans/BCSans-LightItalic.ttf",
            "/themes/Unity/fonts/BCSans/BCSans-LightItalic.woff",
            "/themes/Unity/fonts/BCSans/BCSans-LightItalic.woff2",
            "/themes/Unity/fonts/BCSans/BCSans-Regular.otf",
            "/themes/Unity/fonts/BCSans/BCSans-Regular.ttf",
            "/themes/Unity/fonts/BCSans/BCSans-Regular.woff",
            "/themes/Unity/fonts/BCSans/BCSans-Regular.woff2"
        });
    }
}
