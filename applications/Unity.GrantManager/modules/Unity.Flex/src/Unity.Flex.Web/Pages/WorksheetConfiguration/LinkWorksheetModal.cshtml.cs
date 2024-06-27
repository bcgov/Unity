using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;

public class LinkWorksheetModalModel() : FlexPageModel
{
    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    [MinLength(3)]
    [MaxLength(25)]
    public string? Title { get; set; }

    [BindProperty]
    public string? Name { get; set; }

    public async Task OnGetAsync(Guid worksheetId)
    {
        Id = worksheetId;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return NoContent();
    }  
}
