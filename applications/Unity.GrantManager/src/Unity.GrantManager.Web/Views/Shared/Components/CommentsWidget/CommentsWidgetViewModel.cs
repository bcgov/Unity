using System;
using System.Collections.Generic;
using Unity.GrantManager.Comments;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.CommentsWidget
{
    public class CommentsWidgetViewModel
    {
        public CommentsWidgetViewModel()
        {
            Comments = new List<CommentDto>();
        }
        public List<GrantApplicationAssigneeDto> AllAssigneeList { get; set; } = new();

        public IReadOnlyList<CommentDto> Comments { get; set; }
        public Guid OwnerId { get; set; }
        public CommentType CommentType { get; set; }
        public Guid CurrentUserId { get; set; }
        public bool CanPinComments { get; set; }
    }
}
