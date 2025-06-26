using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Bundling;

public class UnityThemeUX2GlobalScriptContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        // .NET 9 upgrade wants datatable 2 as a default which has an effect on the file dependencies
        // but we are using the old datatable 1.x version for now - update these removes when move to 
        // datatable 2.x version
        context.Files.Remove("/libs/datatables.net/js/dataTables.min.js");
        context.Files.Remove("/libs/datatables.net-bs5/js/dataTables.bootstrap5.js");
        context.Files.Remove("/libs/abp/aspnetcore-mvc-ui-theme-shared/datatables/datatables-extensions.js");

        context.Files.Add("/themes/ux2/layout.js");
        context.Files.Add("/themes/ux2/table-utils.js");

        context.Files.AddIfNotContains("/libs/pubsub-js/src/pubsub.js");

        context.Files.AddIfNotContains("/libs/datatables.net/js/jquery.dataTables.js");
        context.Files.AddIfNotContains("/libs/datatables.net-bs5/js/dataTables.bootstrap5.min.js");
        context.Files.AddIfNotContains("/libs/abp/aspnetcore-mvc-ui-theme-shared/datatables/datatables-extensions.js");

        context.Files.AddIfNotContains("/libs/jszip/dist/jszip.min.js");

        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/buttons.colVis.min.js");
        
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/dataTables.buttons.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/buttons.html5.min.js");

        context.Files.AddIfNotContains("/libs/datatables.net-buttons-bs5/js/buttons.bootstrap5.js");        

        context.Files.AddIfNotContains("/libs/datatables.net-select/js/dataTables.select.js");        
        context.Files.AddIfNotContains("/libs/datatables.net-select-bs5/js/select.bootstrap5.js");

        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader/js/dataTables.fixedHeader.js");
        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader-bs5/js/fixedHeader.bootstrap5.min.js");

        context.Files.AddIfNotContains("/libs/datatables.net-staterestore-dt/js/stateRestore.dataTables.js");
        context.Files.AddIfNotContains("/libs/datatables.net-staterestore/js/dataTables.stateRestore.js");

        context.Files.AddIfNotContains("/libs/datatables.net-colreorder/js/dataTables.colReorder.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-colreorder-bs5/js/colReorder.bootstrap5.min.js");     

        context.Files.AddIfNotContains("/libs/echarts/echarts.min.js");
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
