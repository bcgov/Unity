using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionYesNoDefinitionWidget
{
    public class QuestionYesNoDefinitionViewModel : WorksheetFieldDefinitionViewModelBase
    {
        public QuestionYesNoDefinitionViewModel() : base()
        {
        }

        [DisplayName("Value of Yes")]
        [BindProperty]
        [Required]
        [Range(long.MinValue, long.MaxValue)]
        public long YesValue { get; set; } = 0;

        [DisplayName("Value of No")]
        [BindProperty]
        [Required]
        [Range(long.MinValue, long.MaxValue)]
        public long NoValue { get; set; } = 0;
    }
}
