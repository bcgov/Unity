using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;

public class UpsertSectionModalModel(IWorksheetAppService worksheetAppService, IWorksheetSectionAppService worksheetSectionAppService) : FlexPageModel
{
    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    [MinLength(1)]
    [MaxLength(25)]
    [Required]
    public string? Name { get; set; }

    [BindProperty]
    public Guid WorksheetId { get; set; }

    [BindProperty]
    public Guid? SectionId { get; set; }

    [BindProperty]
    public uint Order { get; set; }

    [BindProperty]
    public WorksheetUpsertAction UpsertAction { get; set; }

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
        switch (UpsertAction)
        {
            case WorksheetUpsertAction.Insert:
                return MapModalResponse(await InsertSectionAsync());
            case WorksheetUpsertAction.Update:
                return MapModalResponse(await UpdateSectionAsync());
            default:
                break;
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
    }
}