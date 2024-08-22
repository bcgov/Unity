using Unity.Flex.Scoresheets;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionDefinitionWidget
{
    public class QuestionDefinitionViewModel
    {
        public QuestionDefinitionViewModel()
        {
        }

        public QuestionType Type { get; set; }
        public string? Definition { get; set; }
    }
}
