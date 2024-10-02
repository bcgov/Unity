using Microsoft.AspNetCore.Mvc;
using System;
using Unity.GrantManager.Comments;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.CommentsWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/Comments")]
    public class CommentsWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("RefreshComments")]
        public IActionResult Comments(Guid ownerId, CommentType commentType, Guid currentUserId)
        { 
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for CommentsWidgetController: RefreshComments");
                return ViewComponent("CommentsWidget");
            }
            return ViewComponent("CommentsWidget", new { ownerId, commentType, currentUserId });
        }
    }
}
