using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Unity.Flex.Web.Views.Shared.Components.QuestionDefinitionWidget;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Volo.Abp.Validation;

namespace Unity.Flex.Web.Pages.ScoresheetConfiguration;

public class QuestionModalModel : FlexPageModel
{
    private readonly IQuestionAppService _questionAppService;
    private readonly IScoresheetAppService _scoresheetAppService;
    public List<SelectListItem> QuestionTypeOptionsList { get; set; }
    public QuestionModalModel(IQuestionAppService questionAppService, IScoresheetAppService scoresheetAppService)
    {
        _questionAppService = questionAppService;
        _scoresheetAppService = scoresheetAppService;
        QuestionTypeOptionsList = Enum.GetValues(typeof(QuestionType))
                                      .Cast<QuestionType>()
                                      .Select(qt => new SelectListItem
                                      {
                                          Value = ((int)qt).ToString(),
                                          Text = GetOptionText(qt)
                                      })
                                      .ToList();
    }

    private static string GetOptionText(QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.YesNo => "Yes/No Select",
            QuestionType.SelectList => "Select List",
            _ => questionType.ToString(),
        };
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

        [Display(Name = "Scoresheet:Configuration:QuestionModal.QuestionType")]
        [SelectItems(nameof(QuestionTypeOptionsList))]
        public string QuestionType { get; set; } = string.Empty;
        public string? Definition { get; set; }

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
            Question.QuestionType = ((int)question.Type).ToString();
            Question.Definition = question.Definition;
        }
        else
        {
            Question.QuestionType = ((int)QuestionType.Number).ToString();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {

        if (Question.ActionType.StartsWith("Edit"))
        {
            await EditQuestion();
            return NoContent();
        }        
        else if (Question.ActionType.StartsWith("Add"))
        {
            await CreateQuestion();
            return NoContent();
        }        
        else if (Question.ActionType.StartsWith("Delete"))
        {
            await DeleteQuestion();
            return NoContent();
        }
        else
        {
            throw new AbpValidationException("Invalid ActionType!");
        }
    }

    private async Task CreateQuestion()
    {
        _ = await _scoresheetAppService.CreateQuestionInHighestOrderSectionAsync(Question.ScoresheetId, new CreateQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description, QuestionType = uint.Parse(Question.QuestionType), Definition = ExtractDefinition() });
    }    

    private async Task EditQuestion()
    {
        await _scoresheetAppService.ValidateChangeableScoresheet(Question.ScoresheetId);
        _ = await _questionAppService.UpdateAsync(Question.Id, new EditQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description, QuestionType = uint.Parse(Question.QuestionType), Definition = ExtractDefinition() });
    }
    
    private async Task DeleteQuestion()
    {
        await _scoresheetAppService.ValidateChangeableScoresheet(Question.ScoresheetId);
        await _questionAppService.DeleteAsync(Question.Id);
    }

    private object? ExtractDefinition()
    {
        var questionType = Enum.TryParse(Question.QuestionType, out QuestionType type);
        if (!questionType) return null;
        return QuestionDefinitionWidget.ParseFormValues(type, Request.Form);
    }
}
