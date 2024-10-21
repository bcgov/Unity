using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Unity.Flex.Scoresheets;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionSelectListDefinitionWidget
{
    public class QuestionSelectListDefinitionViewModel : RequiredFieldViewModel
    {     
        [BindProperty]
        public List<QuestionSelectListOptionDto> Options { get; set; } = [];
    }
}
