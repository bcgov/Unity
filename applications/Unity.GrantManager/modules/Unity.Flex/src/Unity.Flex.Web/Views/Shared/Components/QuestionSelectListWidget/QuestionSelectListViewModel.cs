using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionSelectListWidget
{
    public class QuestionSelectListViewModel
    {
        public Guid QuestionId { get; set; }
        public bool IsDisabled { get; set; }
        public string Answer { get; set; } = string.Empty;
        public string Definition { get; set; } = "{}";
    }
}
