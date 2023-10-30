using Microsoft.AspNetCore.Mvc;
using System;
using Unity.GrantManager.Comments;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.CommentsWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/Comments")]
    public class CommentsWidgetController : AbpController
    {
        [HttpGet]
        [Route("RefreshComments")]
        public IActionResult Comments(Guid ownerId, CommentType commentType, Guid currentUserId)
        { 
            return ViewComponent("CommentsWidget", new { ownerId, commentType, currentUserId });
        }
    }
}
