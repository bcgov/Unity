using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Unity.Flex.Web.Pages.WorksheetConfiguration;

public class UpsertWorksheetLinksModalModel() : FlexPageModel
{
    [BindProperty]
    public Guid WorksheetId { get; set; }

    public List<SelectListItem>? UiAnchors { get; private set; }

    public async Task OnGetAsync(Guid worksheetId)
    {
        WorksheetId = worksheetId;

        UiAnchors =
        [
            new SelectListItem("Project Info","ProjectInfo"),
            new SelectListItem("Applicant Info","ApplicantInfo"),
            new SelectListItem("Assessment Info","AssessmentInfo"),
        ];

        await Task.CompletedTask;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await Task.CompletedTask;
        return NoContent();
    }
}
