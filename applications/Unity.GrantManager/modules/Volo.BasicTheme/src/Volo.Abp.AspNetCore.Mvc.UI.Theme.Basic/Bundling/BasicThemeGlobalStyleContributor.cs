using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Bundling;

public class BasicThemeGlobalStyleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add("/themes/basic/fluentui-icons.css");
        context.Files.Add("/themes/basic/fluenticons.min.css");
        context.Files.Add("/themes/basic/fonts.css");
        context.Files.Add("/themes/basic/layout.css");
        context.Files.Add("/themes/basic/unity-styles.css");

        // Add assets for "/themes/basic/fonts/**/*"
        context.Files.AddRange(new[] {
            "/themes/basic/fonts/icons/Segoe-Fluent-Icons.ttf",
            "/themes/basic/fonts/icons/Segoe-MDL2-Assets.ttf",
            "/themes/basic/fonts/BCSans/BCSans-Bold.otf",
            "/themes/basic/fonts/BCSans/BCSans-Bold.ttf",
            "/themes/basic/fonts/BCSans/BCSans-Bold.woff",
            "/themes/basic/fonts/BCSans/BCSans-Bold.woff2",
            "/themes/basic/fonts/BCSans/BCSans-BoldItalic.otf",
            "/themes/basic/fonts/BCSans/BCSans-BoldItalic.ttf",
            "/themes/basic/fonts/BCSans/BCSans-BoldItalic.woff",
            "/themes/basic/fonts/BCSans/BCSans-BoldItalic.woff2",
            "/themes/basic/fonts/BCSans/BCSans-Italic.otf",
            "/themes/basic/fonts/BCSans/BCSans-Italic.ttf",
            "/themes/basic/fonts/BCSans/BCSans-Italic.woff",
            "/themes/basic/fonts/BCSans/BCSans-Italic.woff2",
            "/themes/basic/fonts/BCSans/BCSans-Light.otf",
            "/themes/basic/fonts/BCSans/BCSans-Light.ttf",
            "/themes/basic/fonts/BCSans/BCSans-Light.woff",
            "/themes/basic/fonts/BCSans/BCSans-Light.woff2",
            "/themes/basic/fonts/BCSans/BCSans-LightItalic.otf",
            "/themes/basic/fonts/BCSans/BCSans-LightItalic.ttf",
            "/themes/basic/fonts/BCSans/BCSans-LightItalic.woff",
            "/themes/basic/fonts/BCSans/BCSans-LightItalic.woff2",
            "/themes/basic/fonts/BCSans/BCSans-Regular.otf",
            "/themes/basic/fonts/BCSans/BCSans-Regular.ttf",
            "/themes/basic/fonts/BCSans/BCSans-Regular.woff",
            "/themes/basic/fonts/BCSans/BCSans-Regular.woff2"
        });
    }
}
