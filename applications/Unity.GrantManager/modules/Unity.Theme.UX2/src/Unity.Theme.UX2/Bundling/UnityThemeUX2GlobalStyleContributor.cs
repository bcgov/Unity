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
        context.Files.Add("/themes/ux2/plugins/tableContextMenu.css");
        context.Files.Add("/themes/ux2/json-editor.css");

        context.Files.AddIfNotContains("/libs/datatables.net-bs5/css/dataTables.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons-bs5/css/buttons.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-select-bs5/css/select.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-colreorder-bs5/css/colReorder.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader-bs5/css/fixedHeader.bootstrap5.min.css");
        context.Files.AddIfNotContains("/libs/datatables.net-staterestore-dt/css/stateRestore.dataTables.min.css");
        context.Files.AddIfNotContains("/libs/tributejs/dist/tribute.css");

        // ABP's own FontAwesomeStyleContributor adds the v4 compatibility shims
        // alongside all.css. Every rule in that file is scoped ".fa.fa-x", and no
        // element in this app carries the bare "fa" class any more (AB#33942), so
        // the shims can no longer match anything. Base bundle contributors run
        // before this one, so the file is present by the time we remove it.
        context.Files.RemoveAll(f =>
            f.FileName == "/libs/@fortawesome/fontawesome-free/css/v4-shims.css");
    }
}
