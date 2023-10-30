using System;
using System.Collections.Generic;
using Unity.GrantManager.Comments;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form.DatePicker;

namespace Unity.GrantManager.Web.Views.Shared.Components.CommentsWidget
{
    public class CommentsWidgetViewModel
    {
        public CommentsWidgetViewModel()
        {
            Comments = new List<CommentViewModel>();
        }

        public IReadOnlyList<CommentViewModel> Comments { get; set; }
        public Guid OwnerId { get; set; }
        public CommentType CommentType { get; set; }
        public Guid CurrentUserId { get; set; }
    }

    public class CommentViewModel : CommentDto
    {        
        public string Badge { get; set; } = string.Empty;
    }
}
