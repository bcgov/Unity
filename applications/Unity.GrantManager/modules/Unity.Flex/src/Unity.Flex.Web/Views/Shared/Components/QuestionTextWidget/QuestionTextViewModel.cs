using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionTextWidget
{
    public class QuestionTextViewModel : RequiredFieldViewModel
    {
        public Guid QuestionId { get; set; }
        public bool IsDisabled { get; set; }
        public string Answer { get; set; } = string.Empty;
        public string? MinLength { get; set; }
        public string? MaxLength { get; set; }
        public bool IsHumanConfirmed { get; set; } = true;
        public string? AICitation { get; set; }
        public int? AIConfidence { get; set; }
    }
}
