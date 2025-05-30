using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Bundling;

public class UnityThemeUX2GlobalStyleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add("/themes/ux2/fonts.css");
        context.Files.Add("/themes/ux2/fluentui-icons.css");
        context.Files.Add("/themes/ux2/fluenticons.min.css");
        context.Files.Add("/themes/ux2/layout.css");
        context.Files.Add("/themes/ux2/unity-styles.css");

        context.Files.AddIfNotContains("/libs/datatables.net-bs5/css/dataTables.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons-bs5/css/buttons.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-select-bs5/css/select.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-colreorder-bs5/css/colReorder.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader-bs5/css/fixedHeader.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-staterestore-dt/css/stateRestore.dataTables.min.css");
        context.Files.AddIfNotContains("/libs/tributejs/dist/tribute.css");

        // Add assets for "/themes/ux2/fonts/**/*"
        context.Files.AddRange([
            "/themes/ux2/fonts/icons/Segoe-Fluent-Icons.ttf",
            "/themes/ux2/fonts/icons/Segoe-MDL2-Assets.ttf",
        ]);
    }
}
