using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;

namespace Unity.Flex.Web.Views.Shared.Components.QuestionTextAreaWidget
{
    [Widget(
        RefreshUrl = "Widgets/QuestionTextArea/Refresh",
        AutoInitialize = true)]
    public class QuestionTextAreaWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Guid questionId,
            bool isDisabled,
            string? answer,
            string? minLength,
            string? maxLength,
            uint? rows = 1,
            bool required = false,
            bool isHumanConfirmed = true,
            string? aiCitation = null,
            int? aiConfidence = null)
        {
            return View(await Task.FromResult(new QuestionTextAreaViewModel()
            {
                QuestionId = questionId,
                IsDisabled = isDisabled,
                Answer = answer ?? string.Empty,
                MinLength = minLength,
                MaxLength = maxLength,
                Required = required,
                Rows = rows,
                IsHumanConfirmed = isHumanConfirmed,
                AICitation = aiCitation,
                AIConfidence = aiConfidence
            }));
        }
    }
}
