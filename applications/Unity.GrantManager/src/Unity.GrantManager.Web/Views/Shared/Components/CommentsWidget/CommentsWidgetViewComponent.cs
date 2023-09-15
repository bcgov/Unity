using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.Comments;

namespace Unity.GrantManager.Web.Views.Shared.Components.CommentsWidget
{
    [Widget(
        RefreshUrl = "Widgets/Comments/RefreshComments",
        ScriptTypes = new[] { typeof(CommentsWidgetScriptBundleContributor) },
        StyleTypes = new[] { typeof(CommentsWidgetStyleBundleContributor) },
        AutoInitialize = true)]
    public class CommentsWidgetViewComponent : AbpViewComponent
    {
        private readonly CommentAppService _commentAppService;

        public CommentsWidgetViewComponent(CommentAppService commentAppService)
        {
            _commentAppService = commentAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid ownerId, CommentType commentType)
        {            
            CommentsWidgetViewModel model = new()
            {
                CommentType = commentType,
                OwnerId = ownerId,
                Comments = (await _commentAppService.GetListAsync(new QueryCommentsByTypeDto() { OwnerId = ownerId, CommentType = commentType })).ToList()
            };

            return View(model);            
        }
    }

    public class CommentsWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CommentsWidget/Default.css");
        }
    }

    public class CommentsWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CommentsWidget/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
