using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Bundling;

public class UnityThemeUX2GlobalStyleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add("/themes/ux2/fluentui-icons.css");
        context.Files.Add("/themes/ux2/fluenticons.min.css");
        context.Files.Add("/themes/ux2/fonts.css");
        context.Files.Add("/themes/ux2/layout.css");
        context.Files.Add("/themes/ux2/unity-styles.css");

        context.Files.AddIfNotContains("/libs/datatables.net-bs5/css/dataTables.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons-bs5/css/buttons.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-select-bs5/css/select.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-colreorder-bs5/css/colReorder.bootstrap5.css");
        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader-bs5/css/fixedHeader.bootstrap5.css");

        // Add assets for "/themes/ux2/fonts/**/*"
        context.Files.AddRange(new List<BundleFile> {
            "/themes/ux2/fonts/icons/Segoe-Fluent-Icons.ttf",
            "/themes/ux2/fonts/icons/Segoe-MDL2-Assets.ttf",
            "/themes/ux2/fonts/BCSans/BCSans-Bold.otf",
            "/themes/ux2/fonts/BCSans/BCSans-Bold.ttf",
            "/themes/ux2/fonts/BCSans/BCSans-Bold.woff",
            "/themes/ux2/fonts/BCSans/BCSans-Bold.woff2",
            "/themes/ux2/fonts/BCSans/BCSans-BoldItalic.otf",
            "/themes/ux2/fonts/BCSans/BCSans-BoldItalic.ttf",
            "/themes/ux2/fonts/BCSans/BCSans-BoldItalic.woff",
            "/themes/ux2/fonts/BCSans/BCSans-BoldItalic.woff2",
            "/themes/ux2/fonts/BCSans/BCSans-Italic.otf",
            "/themes/ux2/fonts/BCSans/BCSans-Italic.ttf",
            "/themes/ux2/fonts/BCSans/BCSans-Italic.woff",
            "/themes/ux2/fonts/BCSans/BCSans-Italic.woff2",
            "/themes/ux2/fonts/BCSans/BCSans-Light.otf",
            "/themes/ux2/fonts/BCSans/BCSans-Light.ttf",
            "/themes/ux2/fonts/BCSans/BCSans-Light.woff",
            "/themes/ux2/fonts/BCSans/BCSans-Light.woff2",
            "/themes/ux2/fonts/BCSans/BCSans-LightItalic.otf",
            "/themes/ux2/fonts/BCSans/BCSans-LightItalic.ttf",
            "/themes/ux2/fonts/BCSans/BCSans-LightItalic.woff",
            "/themes/ux2/fonts/BCSans/BCSans-LightItalic.woff2",
            "/themes/ux2/fonts/BCSans/BCSans-Regular.otf",
            "/themes/ux2/fonts/BCSans/BCSans-Regular.ttf",
            "/themes/ux2/fonts/BCSans/BCSans-Regular.woff",
            "/themes/ux2/fonts/BCSans/BCSans-Regular.woff2"
        });
    }
}
