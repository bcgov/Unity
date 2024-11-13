using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionNumberWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/QuestionNumber")]
    public class CurrencyWidgetController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(Guid questionId, bool isDisabled, double? answer, int? min, int? max)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                Logger.LogWarning("Invalid model state for CurrencyWidgetController:Refresh");
                return ViewComponent(typeof(QuestionNumberWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(QuestionNumberWidget), new { questionId, isDisabled, answer, min, max });
        }
    }
}
