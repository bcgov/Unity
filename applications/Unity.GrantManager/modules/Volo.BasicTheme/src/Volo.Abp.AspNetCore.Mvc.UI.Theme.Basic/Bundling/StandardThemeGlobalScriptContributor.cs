﻿using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace Unity.AspNetCore.Mvc.UI.Themes.Standard.Bundling;

public class StandardThemeGlobalScriptContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add("/themes/standard/layout.js");

        context.Files.AddIfNotContains("/libs/pubsub-js/src/pubsub.js");

        context.Files.AddIfNotContains("/libs/datatables.net-bs5/js/dataTables.bootstrap5.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/dataTables.buttons.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/buttons.colVis.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons/js/buttons.html5.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-select/js/dataTables.select.js");
        context.Files.AddIfNotContains("/libs/datatables.net-buttons-bs5/js/buttons.bootstrap5.js");
        context.Files.AddIfNotContains("/libs/datatables.net-select-bs5/js/select.bootstrap5.js");
        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader/js/dataTables.fixedHeader.js");
        context.Files.AddIfNotContains("/libs/chart.js/dist/chart.umd.js");

        context.Files.AddIfNotContains("/libs/datatables.net-colreorder/js/dataTables.colReorder.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-colreorder-bs5/js/colReorder.bootstrap5.min.js");
        context.Files.AddIfNotContains("/libs/datatables.net-fixedheader-bs5/js/fixedHeader.bootstrap5.min.js");
        context.Files.AddIfNotContains("/libs/echarts/echarts.min.js");
    }
}