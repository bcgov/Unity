using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionTextWidget
{
    public class QuestionTextViewModel 
    {
        public Guid QuestionId { get; set; }
        public bool IsDisabled { get; set; }
        public string Answer { get; set; } = string.Empty;
        public string? MinLength { get; set; }
        public string? MaxLength { get; set; }
    }
}
