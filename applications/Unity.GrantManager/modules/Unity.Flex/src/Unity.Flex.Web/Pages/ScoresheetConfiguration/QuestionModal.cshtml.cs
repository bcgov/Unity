using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Volo.Abp.Validation;

namespace Unity.Flex.Web.Pages.ScoresheetConfiguration;

public class QuestionModalModel : FlexPageModel
{
    private readonly IScoresheetAppService _scoresheetAppService;

    public QuestionModalModel(IScoresheetAppService scoresheetAppService)
    {
        _scoresheetAppService = scoresheetAppService;
    }

    [BindProperty]
    public QuestionModalModelModel Question { get; set; } = new();

    public class QuestionModalModelModel
    {
        public Guid Id { get; set; }
        public string ActionType { get; set; } = string.Empty;
        [Display(Name = "Scoresheet:Configuration:QuestionModal.Name")]
        public string Name { get; set; } = string.Empty;
        [Display(Name = "Scoresheet:Configuration:QuestionModal.Label")]
        public string Label { get; set; } = string.Empty;
    }
    public async Task OnGetAsync(Guid questionId,
       string actionType)
    {
        Question.Id = questionId;
        Question.ActionType = actionType;
        if (Question.ActionType.Contains("Edit"))
        {
            QuestionDto question = await _scoresheetAppService.GetQuestionAsync(questionId);
            Question.Name = question.Name ?? "";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Question.ActionType.StartsWith("Edit"))
        {
            //await EditQuestion();

            return NoContent();
        }
        else if (Question.ActionType.StartsWith("Add"))
        {
            await CreateQuestion();

            return NoContent();
        }
        else
        {
            throw new AbpValidationException("Invalid ActionType!");
        }
    }

    private async Task CreateQuestion()
    {
        _ = await _scoresheetAppService.CreateQuestionAsync(new CreateQuestionDto() { Name = Question.Name });
    }
}
