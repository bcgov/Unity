using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;

public class UpsertWorksheetModalModel(IWorksheetAppService worksheetAppService) : FlexPageModel
{
    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    [MinLength(3)]
    [MaxLength(25)]
    public string? Title { get; set; }

    [BindProperty]
    public string? Name { get; set; }

    [BindProperty]
    [SelectItems(nameof(UiAnchors))]
    public string? UiAnchor { get; set; }

    [BindProperty]
    public WorksheetUpsertAction UpsertAction { get; set; }

    public List<SelectListItem> UiAnchors { get; set; } = [];

    public async Task OnGetAsync(Guid worksheetId, string actionType)
    {
        UiAnchors =
        [
            new SelectListItem("Project Info","ProjectInfo"),
            new SelectListItem("Applicant Info","ApplicantInfo"),
            new SelectListItem("Assessment Info","AssessmentInfo"),
        ];

        UpsertAction = (WorksheetUpsertAction)Enum.Parse(typeof(WorksheetUpsertAction), actionType);

        if (UpsertAction == WorksheetUpsertAction.Update)
        {
            WorksheetDto worksheetDto = await worksheetAppService.GetAsync(worksheetId);
            UpsertAction = (WorksheetUpsertAction)Enum.Parse(typeof(WorksheetUpsertAction), actionType);
            
            Name = worksheetDto.Name;
            UiAnchor = worksheetDto.UiAnchor;
            Id = worksheetDto.Id;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        switch (UpsertAction)
        {
            case WorksheetUpsertAction.Insert:
                return MapModalResponse(await worksheetAppService.CreateAsync(MapWorksheetModel()));
            case WorksheetUpsertAction.Update:
                return MapModalResponse(await worksheetAppService.CreateAsync(MapWorksheetModel()));
            case WorksheetUpsertAction.VersionUp:
                break;
        }

        return NoContent();
    }

    private CreateWorksheetDto MapWorksheetModel()
    {
        if (Name == null || Title == null)
        {
            throw new UserFriendlyException("Invalid worksheet information captured");
        }

        return new CreateWorksheetDto()
        {
            Name = Name,
            Title = Title,            
            Sections = [],
        };
    }

    private static OkObjectResult MapModalResponse(WorksheetDto worksheetDto)
    {
        return new OkObjectResult(new ModalResponse()
        {
            WorksheetId = worksheetDto.Id
        });
    }

    public class ModalResponse
    {
        public Guid WorksheetId { get; set; }
    }

    //private async Task CreateScoresheet()
    //{
    //    _ = await _scoresheetAppService.CreateAsync(new CreateScoresheetDto() { Name = Scoresheet.Name });
    //}

    //private async Task EditScoresheets()
    //{
    //    await _scoresheetAppService.UpdateAllAsync(Scoresheet.GroupId, new EditScoresheetsDto() { Name = Scoresheet.Name, ActionType = Scoresheet.ActionType });
    //}

    //private async Task EditScoresheetsAndCreateNewVersion()
    //{
    //    await _scoresheetAppService.UpdateAllAsync(Scoresheet.GroupId, new EditScoresheetsDto() { Name = Scoresheet.Name, ActionType = Scoresheet.ActionType });
    //    _ = await _scoresheetAppService.CloneScoresheetAsync(Scoresheet.Id, null, null);
    //}

    //private async Task DeleteScoresheet()
    //{
    //    await _scoresheetAppService.DeleteAsync(Scoresheet.Id);
    //}
}
