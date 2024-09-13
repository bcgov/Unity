using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;

public class CloneWorksheetModalModel(IWorksheetListAppService worksheetListAppService,
    IWorksheetAppService worksheetAppService) : FlexPageModel
{
    [BindProperty]
    public Guid WorksheetId { get; set; }

    [BindProperty]
    public string? Title { get; set; }

    [BindProperty]
    public string? Name { get; set; }

    public async Task OnGetAsync(Guid worksheetId)
    {
        var worksheetDto = await worksheetListAppService.GetAsync(worksheetId);

        WorksheetId = worksheetId;
        Title = worksheetDto.Title;
        Name = worksheetDto.Name;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _ = await worksheetAppService.CloneAsync(WorksheetId);
        return NoContent();
    }
}
