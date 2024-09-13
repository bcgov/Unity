using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionYesNoWidget
{
    public class QuestionYesNoViewModel 
    {
        public Guid QuestionId { get; set; }
        public bool IsDisabled { get; set; }
        public string Answer { get; set; } = string.Empty;
        public string? YesValue { get; set; }
        public string? NoValue { get; set; }
    }
}
