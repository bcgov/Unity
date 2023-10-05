using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Volo.Abp.AspNetCore.Mvc.UI.Theme.Basic.Bundling;

public class BasicThemeGlobalScriptContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add("/themes/basic/layout.js");

        context.Files.AddIfNotContains("/libs/pubsub-js/src/pubsub.js");

        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/dataTables.buttons.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/buttons.colVis.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/buttons.html5.min.js");

        context.Files.AddIfNotContains("/libs/datatables.net-buttons-bs5/js/buttons.bootstrap5.js");
        context.Files.AddIfNotContains("/libs/datatables.net-select/js/dataTables.select.js");
        context.Files.AddIfNotContains("/libs/datatables.net-select-bs5/js/select.bootstrap5.js");
    }
}
