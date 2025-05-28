using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Bundling;

public class UnityThemeUX2GlobalScriptContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add("/themes/ux2/layout.js");
        context.Files.Add("/themes/ux2/table-utils.js");

        context.Files.AddIfNotContains("/libs/pubsub-js/src/pubsub.js");

        context.Files.AddIfNotContains("/libs/datatables.net-bs5/js/dataTables.bootstrap5.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/dataTables.buttons.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/buttons.colVis.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/buttons.html5.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-select/js/dataTables.select.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons-bs5/js/buttons.bootstrap5.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-select-bs5/js/select.bootstrap5.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader/js/dataTables.fixedHeader.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-staterestore-dt/js/stateRestore.dataTables.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-staterestore/js/dataTables.stateRestore.min.js");

        context.Files.AddIfNotContains("/libs/datatables.net-colreorder/js/dataTables.colReorder.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-colreorder-bs5/js/colReorder.bootstrap5.min.js");

        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader-bs5/js/fixedHeader.bootstrap5.min.js");
        context.Files.AddIfNotContains("/libs/jszip/dist/jszip.min.js");
        context.Files.AddIfNotContains("/libs/tributejs/dist/tribute.min.js");

        context.Files.AddIfNotContains("/libs/tinymce/tinymce.min.js");
        context.Files.AddIfNotContains("/libs/tinymce/themes/silver/theme.min.js");
        context.Files.AddIfNotContains("/libs/tinymce/plugins/lists/plugin.min.js");
        context.Files.AddIfNotContains("/libs/tinymce/icons/default/icons.min.js");
        context.Files.AddIfNotContains("/libs/tinymce/models/dom/model.min.js");
        context.Files.AddIfNotContains("/libs/tinymce/plugins/link/plugin.min.js");
        context.Files.AddIfNotContains("/libs/tinymce/plugins/image/plugin.min.js");
        context.Files.AddIfNotContains("/libs/tinymce/plugins/preview/plugin.min.js");
        context.Files.AddIfNotContains("/libs/tinymce/plugins/code/plugin.min.js");
        context.Files.AddIfNotContains("/libs/handlebars/dist/handlebars.min.js");

    }
}
