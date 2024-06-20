using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Volo.Abp.Validation;

namespace Unity.Flex.Web.Pages.ScoresheetConfiguration;

public class QuestionModalModel : FlexPageModel
{
    private readonly IQuestionAppService _questionAppService;
    private readonly IScoresheetAppService _scoresheetAppService;

    public QuestionModalModel(IQuestionAppService questionAppService, IScoresheetAppService scoresheetAppService)
    {
        _questionAppService = questionAppService;
        _scoresheetAppService = scoresheetAppService;
    }

    [BindProperty]
    public QuestionModalModelModel Question { get; set; } = new();

    public class QuestionModalModelModel
    {
        public Guid Id { get; set; }
        public Guid ScoresheetId { get; set; }
        public Guid SectionId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        [Display(Name = "Scoresheet:Configuration:QuestionModal.Name")]
        public string Name { get; set; } = string.Empty;
        [Display(Name = "Scoresheet:Configuration:QuestionModal.Label")]
        public string Label { get; set; } = string.Empty;
        [Display(Name = "Scoresheet:Configuration:QuestionModal.Description")]
        public string? Description { get; set; }
    }
    public async Task OnGetAsync(Guid scoresheetId, Guid sectionId, Guid questionId,
       string actionType)
    {
        Question.Id = questionId;
        Question.ScoresheetId = scoresheetId;
        Question.SectionId = sectionId;
        Question.ActionType = actionType;
        if (Question.ActionType.Contains("Edit"))
        {
            QuestionDto question = await _questionAppService.GetAsync(questionId);
            Question.Name = question.Name ?? "";
            Question.Label = question.Label ?? "";
            Question.Description = question.Description ?? "";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Question.ActionType.Equals("Edit Question On Current Version"))
        {
            await EditQuestionOnCurrentVersion();
            return NoContent();
        }
        else if (Question.ActionType.Equals("Edit Question On New Version"))
        {
            await EditQuestionOnNewVersion();
            return NoContent();
        }
        else if (Question.ActionType.Equals("Add Question On Current Version"))
        {
            await CreateQuestionOnCurrentVersion();
            return NoContent();
        }
        else if (Question.ActionType.Equals("Add Question On New Version"))
        {
            await CreateQuestionOnNewVersion();
            return NoContent();
        }
        else if (Question.ActionType.Equals("Delete Question On Current Version"))
        {
            await DeleteQuestionOnCurrentVersion();
            return NoContent();
        }
        else if (Question.ActionType.Equals("Delete Question On New Version"))
        {
            await DeleteQuestionOnNewVersion();
            return NoContent();
        }
        else
        {
            throw new AbpValidationException("Invalid ActionType!");
        }
    }

    private async Task CreateQuestionOnCurrentVersion()
    {
        _ = await _scoresheetAppService.CreateQuestionInHighestOrderSectionAsync(Question.ScoresheetId, new CreateQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description });
    }

    private async Task CreateQuestionOnNewVersion()
    {
        var clone = await _scoresheetAppService.CloneScoresheetAsync(Question.ScoresheetId, Question.SectionId, Question.Id);
        _ = await _scoresheetAppService.CreateQuestionInHighestOrderSectionAsync(clone.ScoresheetId, new CreateQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description });
    }

    private async Task EditQuestionOnCurrentVersion()
    {
        _ = await _questionAppService.UpdateAsync(Question.Id, new EditQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description });
    }

    private async Task EditQuestionOnNewVersion()
    {
        var clone = await _scoresheetAppService.CloneScoresheetAsync(Question.ScoresheetId, Question.SectionId, Question.Id);
        _ = await _questionAppService.UpdateAsync(clone.QuestionId ?? Guid.Empty, new EditQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description });
    }

    private async Task DeleteQuestionOnCurrentVersion()
    {
        await _questionAppService.DeleteAsync(Question.Id);
    }

    private async Task DeleteQuestionOnNewVersion()
    {
        var clone = await _scoresheetAppService.CloneScoresheetAsync(Question.ScoresheetId, Question.SectionId, Question.Id);
        await _questionAppService.DeleteAsync(clone.QuestionId ?? Guid.Empty);
    }
}
