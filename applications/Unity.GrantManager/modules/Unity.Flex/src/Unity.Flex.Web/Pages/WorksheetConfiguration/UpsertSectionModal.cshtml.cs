using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;

public class UpsertSectionModalModel(IWorksheetAppService worksheetAppService,
    IWorksheetSectionAppService worksheetSectionAppService) : FlexPageModel
{
    [BindProperty]
    [MinLength(1)]
    [MaxLength(25)]
    [Required]
    [RegularExpression("^[A-Za-z0-9- ]+$", ErrorMessage = "Illegal character found in name. Please enter a valid name")]    
    public string? Name { get; set; }

    [BindProperty]
    public Guid WorksheetId { get; set; }

    [BindProperty]
    public Guid? SectionId { get; set; }

    [BindProperty]
    public uint Order { get; set; }

    [BindProperty]
    public WorksheetUpsertAction UpsertAction { get; set; }

    [BindProperty]
    public bool IsDelete { get; set; }

    [BindProperty]
    public bool Published { get; set; }

    public async Task OnGetAsync(Guid worksheetId, Guid sectionId, string actionType)
    {
        WorksheetId = worksheetId;
        SectionId = sectionId;
        UpsertAction = (WorksheetUpsertAction)Enum.Parse(typeof(WorksheetUpsertAction), actionType);

        if (UpsertAction == WorksheetUpsertAction.Update)
        {
            // Get the section 
            var section = await worksheetSectionAppService.GetAsync(sectionId);
            Name = section.Name;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var delete = Request.Form["deleteSectionBtn"];
        var save = Request.Form["saveSectionBtn"];


        if (delete == "delete" || IsDelete)
        {
            await worksheetSectionAppService.DeleteAsync(SectionId!.Value);
            return new OkObjectResult(new ModalResponse()
            {
                WorksheetId = WorksheetId,
                Action = "Delete"
            });
        }
        else if (save == "save")
        {
            switch (UpsertAction)
            {
                case WorksheetUpsertAction.Insert:
                    return MapModalResponse(await InsertSectionAsync());
                case WorksheetUpsertAction.Update:
                    return MapModalResponse(await UpdateSectionAsync());
                default:
                    break;
            }
        }

        return NoContent();
    }

    private OkObjectResult MapModalResponse(WorksheetSectionDto worksheetSectionDto)
    {
        return new OkObjectResult(new ModalResponse()
        {
            Id = worksheetSectionDto.Id,
            Name = worksheetSectionDto.Name,
            WorksheetId = WorksheetId,
            Order = worksheetSectionDto.Order
        });
    }

    private async Task<WorksheetSectionDto> InsertSectionAsync()
    {
        return await worksheetAppService.CreateSectionAsync(WorksheetId, new CreateSectionDto()
        {
            Name = Name ?? string.Empty
        });
    }

    private async Task<WorksheetSectionDto> UpdateSectionAsync()
    {
        return await worksheetSectionAppService.EditAsync(SectionId!.Value, new EditSectionDto()
        {
            Name = Name ?? string.Empty
        });
    }

    public class ModalResponse : WorksheetSectionDto
    {
        public Guid WorksheetId { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}
