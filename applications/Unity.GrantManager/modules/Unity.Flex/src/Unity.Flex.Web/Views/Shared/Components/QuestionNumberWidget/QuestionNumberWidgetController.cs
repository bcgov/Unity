using Microsoft.AspNetCore.Mvc;
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
            return ViewComponent(typeof(QuestionNumberWidget), new { questionId, isDisabled, answer, min, max });
        }
    }
}
