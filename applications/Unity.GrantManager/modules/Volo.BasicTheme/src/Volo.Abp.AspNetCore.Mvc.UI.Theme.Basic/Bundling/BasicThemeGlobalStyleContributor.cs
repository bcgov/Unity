using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Bundling;

public class BasicThemeGlobalStyleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add("/themes/standard/fluentui-icons.css");
        context.Files.Add("/themes/standard/fluenticons.min.css");
        context.Files.Add("/themes/standard/fonts.css");
        context.Files.Add("/themes/standard/layout.css");
        context.Files.Add("/themes/standard/unity-styles.css");

        context.Files.AddIfNotContains("/libs/datatables.net-bs5/css/dataTables.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons-bs5/css/buttons.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-select-bs5/css/select.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-colreorder-bs5/css/colReorder.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader-bs5/css/fixedHeader.bootstrap5.css");

        // Add assets for "/themes/standard/fonts/**/*"
        context.Files.AddRange(new[] {
            "/themes/standard/fonts/icons/Segoe-Fluent-Icons.ttf",
            "/themes/standard/fonts/icons/Segoe-MDL2-Assets.ttf",
            "/themes/standard/fonts/BCSans/BCSans-Bold.otf",
            "/themes/standard/fonts/BCSans/BCSans-Bold.ttf",
            "/themes/standard/fonts/BCSans/BCSans-Bold.woff",
            "/themes/standard/fonts/BCSans/BCSans-Bold.woff2",
            "/themes/standard/fonts/BCSans/BCSans-BoldItalic.otf",
            "/themes/standard/fonts/BCSans/BCSans-BoldItalic.ttf",
            "/themes/standard/fonts/BCSans/BCSans-BoldItalic.woff",
            "/themes/standard/fonts/BCSans/BCSans-BoldItalic.woff2",
            "/themes/standard/fonts/BCSans/BCSans-Italic.otf",
            "/themes/standard/fonts/BCSans/BCSans-Italic.ttf",
            "/themes/standard/fonts/BCSans/BCSans-Italic.woff",
            "/themes/standard/fonts/BCSans/BCSans-Italic.woff2",
            "/themes/standard/fonts/BCSans/BCSans-Light.otf",
            "/themes/standard/fonts/BCSans/BCSans-Light.ttf",
            "/themes/standard/fonts/BCSans/BCSans-Light.woff",
            "/themes/standard/fonts/BCSans/BCSans-Light.woff2",
            "/themes/standard/fonts/BCSans/BCSans-LightItalic.otf",
            "/themes/standard/fonts/BCSans/BCSans-LightItalic.ttf",
            "/themes/standard/fonts/BCSans/BCSans-LightItalic.woff",
            "/themes/standard/fonts/BCSans/BCSans-LightItalic.woff2",
            "/themes/standard/fonts/BCSans/BCSans-Regular.otf",
            "/themes/standard/fonts/BCSans/BCSans-Regular.ttf",
            "/themes/standard/fonts/BCSans/BCSans-Regular.woff",
            "/themes/standard/fonts/BCSans/BCSans-Regular.woff2"
        });
    }
}
