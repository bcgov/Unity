using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionTextWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/QuestionText")]
    public class QuestionTextWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(Guid questionId, bool isDisabled, string? answer, int? minLength, int? maxLength)
        {
            return ViewComponent(typeof(QuestionTextWidget), new { questionId, isDisabled, answer, minLength, maxLength });
        }
    }
}
