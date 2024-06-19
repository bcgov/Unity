using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;
public class UpsertCustomFieldModalModel(ICustomFieldAppService customFieldAppService) : FlexPageModel
{
    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public string? Name { get; set; }

    [BindProperty]
    public string? Label { get; set; }

    [BindProperty]
    public uint Order { get; set; }

    [BindProperty]
    public WorksheetUpsertAction UpsertAction { get; set; }

    public async Task OnGetAsync(Guid customFieldId, WorksheetUpsertAction action)
    {
        if (action == WorksheetUpsertAction.Update)
        {
            CustomFieldDto customField = await customFieldAppService.GetAsync(customFieldId);
            Name = customField.Name;
            Label = customField.Label;
            Id = customFieldId;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        switch (UpsertAction)
        {

        }

        return NoContent();

        //if (Question.ActionType.Equals("Edit Question On Current Version"))
        //{
        //    await EditQuestionOnCurrentVersion();
        //    return NoContent();
        //}
        //else if (Question.ActionType.Equals("Edit Question On New Version"))
        //{
        //    await EditQuestionOnNewVersion();
        //    return NoContent();
        //}
        //else if (Question.ActionType.Equals("Add Question On Current Version"))
        //{
        //    await CreateQuestionOnCurrentVersion();
        //    return NoContent();
        //}
        //else if (Question.ActionType.Equals("Add Question On New Version"))
        //{
        //    await CreateQuestionOnNewVersion();
        //    return NoContent();
        //}
        //else if (Question.ActionType.Equals("Delete Question On Current Version"))
        //{
        //    await DeleteQuestionOnCurrentVersion();
        //    return NoContent();
        //}
        //else if (Question.ActionType.Equals("Delete Question On New Version"))
        //{
        //    await DeleteQuestionOnNewVersion();
        //    return NoContent();
        //}
        //else
        //{
        //    throw new AbpValidationException("Invalid ActionType!");
        //}
    }

    //private async Task CreateQuestionOnCurrentVersion()
    //{
    //    _ = await _scoresheetAppService.CreateQuestionInHighestOrderSectionAsync(Question.ScoresheetId, new CreateQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description });
    //}

    //private async Task CreateQuestionOnNewVersion()
    //{
    //    var clone = await _scoresheetAppService.CloneScoresheetAsync(Question.ScoresheetId, Question.SectionId, Question.Id);
    //    _ = await _scoresheetAppService.CreateQuestionInHighestOrderSectionAsync(clone.ScoresheetId, new CreateQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description });
    //}

    //private async Task EditQuestionOnCurrentVersion()
    //{
    //    _ = await _questionAppService.UpdateAsync(Question.Id, new EditQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description });
    //}

    //private async Task EditQuestionOnNewVersion()
    //{
    //    var clone = await _scoresheetAppService.CloneScoresheetAsync(Question.ScoresheetId, Question.SectionId, Question.Id);
    //    _ = await _questionAppService.UpdateAsync(clone.QuestionId ?? Guid.Empty, new EditQuestionDto() { Name = Question.Name, Label = Question.Label, Description = Question.Description });
    //}

    //private async Task DeleteQuestionOnCurrentVersion()
    //{
    //    await _questionAppService.DeleteAsync(Question.Id);
    //}

    //private async Task DeleteQuestionOnNewVersion()
    //{
    //    var clone = await _scoresheetAppService.CloneScoresheetAsync(Question.ScoresheetId, Question.SectionId, Question.Id);
    //    await _questionAppService.DeleteAsync(clone.QuestionId ?? Guid.Empty);
    //}
}
