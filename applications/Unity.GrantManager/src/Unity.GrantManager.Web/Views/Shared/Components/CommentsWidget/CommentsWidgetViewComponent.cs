using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AspNetCore.Mvc.UI.Theme.UX2.Renderers;
using Unity.GrantManager.Comments;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Permissions;
using Unity.Modules.Shared.Utils;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.Identity;
using Volo.Abp.Identity.Integration;

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
        private readonly IAuthorizationService _authorizationService;
        private readonly IMarkdownRenderer _markdownRenderer;
        public string AllAssignees { get; set; } = string.Empty;

        public CommentsWidgetViewComponent(CommentAppService commentAppService,
            BrowserUtils browserUtils,
            IIdentityUserIntegrationService identityUserIntegrationService,
            IAuthorizationService authorizationService,
            IMarkdownRenderer markdownRenderer)
        {
            _commentAppService = commentAppService;
            _browserUtils = browserUtils;
            _identityUserLookupAppService = identityUserIntegrationService;
            _authorizationService = authorizationService;
            _markdownRenderer = markdownRenderer;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid ownerId, CommentType commentType, Guid currentUserId)
        {
            var users = await _identityUserLookupAppService.SearchAsync(new UserLookupSearchInputDto());
            AllAssignees = JsonConvert.SerializeObject(users);

            var commentDtos = await _commentAppService.GetListAsync(new QueryCommentsByTypeDto() { OwnerId = ownerId, CommentType = commentType });
            var mappedComments = MapToCommentDisplay(commentDtos);

            CommentsWidgetViewModel model = new()
            {
                CommentType = commentType,
                OwnerId = ownerId,
                CurrentUserId = currentUserId,
                Comments = mappedComments.ToList(),
                AllAssigneeList = users.Items
                    .Select(user => new GrantApplicationAssigneeDto
                    {
                        Id = user.Id,
                        FullName = $"{user.Name} {user.Surname}",
                        Email = user.Email
                    }).ToList(),
                CanPinComments = await _authorizationService.IsGrantedAsync(GrantApplicationPermissions.Comments.Add)
            };

            return View(model);
        }

        protected virtual IReadOnlyList<CommentDto> MapToCommentDisplay(IReadOnlyList<CommentDto> commentDtos)
        {
            var offset = _browserUtils.GetBrowserOffset();
            var result = new List<CommentDto>(commentDtos.Count);

            foreach (var item in commentDtos)
            {
                result.Add(new CommentDto()
                {
                    Badge = item.Badge,
                    Comment = _markdownRenderer.Render(item.Comment),
                    Commenter = item.Commenter,
                    CreationTime = item.CreationTime.AddMinutes(-offset),
                    CommenterId = item.CommenterId,
                    LastModificationTime = item.LastModificationTime?.AddMinutes(-offset),
                    Id = item.Id,
                    OwnerId = item.OwnerId,
                    PinDateTime = item.PinDateTime?.AddMinutes(-offset)
                });
            }

            return result;
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
