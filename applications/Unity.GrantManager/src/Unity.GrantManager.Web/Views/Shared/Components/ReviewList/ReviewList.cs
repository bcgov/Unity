﻿using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.ReviewList
{

    [Widget(ScriptFiles = new[] { "/libs/datatables/js/dataTables.select.min.js", "/libs/datatables/js/select.bootstrap.min.js" ,"/Views/Shared/Components/ReviewList/ReviewList.js", "/libs/pubsub-js/src/pubsub.js" },
        StyleFiles = new[] { "/Views/Shared/Components/ReviewList/ReviewList.css" })]
    public class ReviewList : AbpViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
