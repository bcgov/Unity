using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionSelectListWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/QuestionSelectList")]
    public class QuestionSelectListWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(Guid questionId, bool isDisabled, string? answer, string definition)
        {
            if (!ModelState.IsValid)
            {
                return ViewComponent(typeof(QuestionSelectListWidget));
            }

            return ViewComponent(typeof(QuestionSelectListWidget), new { questionId, isDisabled, answer, definition });
        }
    }
}
