using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionYesNoWidget
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widgets/QuestionYesNo")]
    public class QuestionYesNoWidgetController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("Refresh")]
        public IActionResult Refresh(Guid questionId, bool isDisabled, string? answer, int? yesValue, int? noValue)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                logger.LogWarning("Invalid model state for QuestionYesNoWidgetController:Refresh");
                return ViewComponent(typeof(QuestionYesNoWidget));
            }

            // If the model state is valid, render the view component
            return ViewComponent(typeof(QuestionYesNoWidget), new { questionId, isDisabled, answer, yesValue, noValue });
        }
    }
}
