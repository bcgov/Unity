using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                Logger.LogWarning("Invalid model state for QuestionTextWidgetController:Refresh");
                return ViewComponent(typeof(QuestionTextWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(QuestionTextWidget), new { questionId, isDisabled, answer, minLength, maxLength });
        }
    }
}
