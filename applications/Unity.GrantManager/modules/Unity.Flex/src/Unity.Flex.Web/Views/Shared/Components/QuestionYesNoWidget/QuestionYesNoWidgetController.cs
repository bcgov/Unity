using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionYesNoWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/QuestionYesNo")]
    public class QuestionYesNoWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(Guid questionId, bool isDisabled, string? answer, int? yesValue, int? noValue)
        {
            return ViewComponent(typeof(QuestionYesNoWidget), new { questionId, isDisabled, answer, yesValue, noValue });
        }
    }
}
