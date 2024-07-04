using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Volo.Abp;

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
    public bool Published { get; set; }

    [BindProperty]
    public WorksheetUpsertAction UpsertAction { get; set; }

    [BindProperty]
    public bool IsDelete { get; set; }

    public async Task OnGetAsync(Guid worksheetId, string actionType)
    {
        UpsertAction = (WorksheetUpsertAction)Enum.Parse(typeof(WorksheetUpsertAction), actionType);

        if (UpsertAction == WorksheetUpsertAction.Update)
        {
            WorksheetDto worksheetDto = await worksheetAppService.GetAsync(worksheetId);
            UpsertAction = (WorksheetUpsertAction)Enum.Parse(typeof(WorksheetUpsertAction), actionType);

            Name = worksheetDto.Name;
            Id = worksheetDto.Id;
            Title = worksheetDto.Title;
            Published = worksheetDto.Published;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var delete = Request.Form["deleteWorksheetBtn"];
        var save = Request.Form["saveWorksheetBtn"];

        if (delete == "delete" || IsDelete == true)
        {
            await worksheetAppService.DeleteAsync(Id);
            return new OkObjectResult(new ModalResponse()
            {
                WorksheetId = Id,
                Action = "Delete"
            });
        }
        else if (save == "save")
        {
            switch (UpsertAction)
            {
                case WorksheetUpsertAction.Insert:
                    return MapModalResponse(await worksheetAppService.CreateAsync(MapCreateWorksheetModel()));
                case WorksheetUpsertAction.Update:
                    return MapModalResponse(await worksheetAppService.EditAsync(Id, MapEditWorksheetModel()));
                default:
                    break;
            }
        }

        return NoContent();
    }

    private CreateWorksheetDto MapCreateWorksheetModel()
    {
        if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Title))
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

    private EditWorksheetDto MapEditWorksheetModel()
    {
        if (Title == null)
        {
            throw new UserFriendlyException("Invalid worksheet information captured");
        }

        return new EditWorksheetDto()
        {
            Title = Title
        };
    }

    private OkObjectResult MapModalResponse(WorksheetDto worksheetDto)
    {
        return new OkObjectResult(new ModalResponse()
        {
            WorksheetId = worksheetDto.Id,
            Action = UpsertAction.ToString()
        });
    }

    public class ModalResponse
    {
        public Guid WorksheetId { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
