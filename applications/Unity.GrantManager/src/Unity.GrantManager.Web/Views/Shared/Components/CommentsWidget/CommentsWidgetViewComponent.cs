using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.Comments;
using Unity.Modules.Shared.Utils;
using Volo.Abp.Identity.Integration;
using Volo.Abp.Identity;
using Newtonsoft.Json;
using Unity.GrantManager.GrantApplications;

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
        private readonly BrowserUtils _browserUtils;
        private readonly IIdentityUserIntegrationService _identityUserLookupAppService;
        public string AllAssignees { get; set; } = string.Empty;

        public CommentsWidgetViewComponent(CommentAppService commentAppService, 
            BrowserUtils browserUtils, IIdentityUserIntegrationService identityUserIntegrationService)
        {
            _commentAppService = commentAppService;
            _browserUtils = browserUtils;
            _identityUserLookupAppService = identityUserIntegrationService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid ownerId, CommentType commentType, Guid currentUserId)
        {
            var users = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
            AllAssignees = JsonConvert.SerializeObject(users);

            CommentsWidgetViewModel model = new()
            {
                CommentType = commentType,
                OwnerId = ownerId,
                CurrentUserId = currentUserId,
                Comments = MapToCommentDisplay(await _commentAppService.GetListAsync(new QueryCommentsByTypeDto() { OwnerId = ownerId, CommentType = commentType })).ToList(),
                AllAssigneeList = users.Items
                .Select(user => new GrantApplicationAssigneeDto
                {
                    Id = user.Id,
                    FullName = $"{user.Name} {user.Surname}",
                    Email = user.Email
                }).ToList()
            };

            return View(model);
        }

        private IEnumerable<CommentDto> MapToCommentDisplay(IReadOnlyList<CommentDto> commentDtos)
        {            
            var offset = _browserUtils.GetBrowserOffset();

            foreach (var item in commentDtos)
            {
                yield return new CommentDto()
                {
                    Badge = item.Badge,
                    Comment = item.Comment,
                    Commenter = item.Commenter,
                    CreationTime = item.CreationTime.AddMinutes(-offset),
                    CommenterId = item.CommenterId,
                    LastModificationTime = item.LastModificationTime?.AddMinutes(-offset),
                    Id = item.Id,
                    OwnerId = item.OwnerId
                };
            }
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
