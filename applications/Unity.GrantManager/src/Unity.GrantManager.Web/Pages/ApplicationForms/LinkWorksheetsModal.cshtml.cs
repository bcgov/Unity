using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.WorksheetLinks;
using Unity.Flex.Worksheets;
using Unity.GrantManager.Flex;
using Unity.Modules.Shared.Correlation;

namespace Unity.GrantManager.Web.Pages.ApplicationForms;

public class LinkWorksheetModalModel(IWorksheetListAppService worksheetListAppService,
    IWorksheetLinkAppService worksheetLinkAppService) : GrantManagerPageModel
{
    [BindProperty]
    public Guid FormVersionId { get; set; }

    [BindProperty]
    public string? FormName { get; set; }

    [BindProperty]
    public string? AssessmentInfoSlotId { get; set; }

    [BindProperty]
    public string? ProjectInfoSlotId { get; set; }

    [BindProperty]
    public string? ApplicantInfoSlotId { get; set; }

    [BindProperty]
    public string? CustomTabsSlotIds { get; set; }

    [BindProperty]
    public List<WorksheetLinkDto>? WorksheetLinks { get; set; }

    [BindProperty]
    public List<WorksheetBasicDto>? PublishedWorksheets { get; set; }

    [BindProperty]
    public WorksheetLinkDto? AssessmentInfoLink { get; set; }

    [BindProperty]
    public WorksheetLinkDto? ApplicantInfoLink { get; set; }

    [BindProperty]
    public WorksheetLinkDto? ProjectInfoLink { get; set; }

    [BindProperty]
    public List<WorksheetLinkDto>? CustomTabLinks { get; set; }

    public async Task OnGetAsync(Guid formVersionId, string formName)
    {
        FormVersionId = formVersionId;
        FormName = formName;

        WorksheetLinks = await worksheetLinkAppService.GetListByCorrelationAsync(formVersionId, CorrelationConsts.FormVersion);

        PublishedWorksheets = [.. (await worksheetListAppService.GetListAsync())
            .Where(s => s.Published && !WorksheetLinks.Select(s => s.WorksheetId).Contains(s.Id))
            .OrderBy(s => s.Title)];

        AssessmentInfoLink = WorksheetLinks.Find(s => s.UiAnchor == FlexConsts.AssessmentInfoUiAnchor);
        AssessmentInfoSlotId = AssessmentInfoLink?.WorksheetId.ToString();

        ProjectInfoLink = WorksheetLinks.Find(s => s.UiAnchor == FlexConsts.ProjectInfoUiAnchor);
        ProjectInfoSlotId = ProjectInfoLink?.WorksheetId.ToString();

        ApplicantInfoLink = WorksheetLinks.Find(s => s.UiAnchor == FlexConsts.ApplicantInfoUiAnchor);
        ApplicantInfoSlotId = ApplicantInfoLink?.WorksheetId.ToString();

        CustomTabLinks = WorksheetLinks.Where(s => s.UiAnchor == FlexConsts.CustomTab).ToList();
        CustomTabsSlotIds = string.Join(";", CustomTabLinks.Select(s => s.Id));
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var tabLinks = new Dictionary<Guid, string>();
        if (AssessmentInfoSlotId != null && AssessmentInfoSlotId != Guid.Empty.ToString())
        {
            tabLinks.Add(Guid.Parse(AssessmentInfoSlotId), FlexConsts.AssessmentInfoUiAnchor);
        }

        if (ProjectInfoSlotId != null && ProjectInfoSlotId != Guid.Empty.ToString())
        {
            tabLinks.Add(Guid.Parse(ProjectInfoSlotId), FlexConsts.ProjectInfoUiAnchor);
        }

        if (ApplicantInfoSlotId != null && ApplicantInfoSlotId != Guid.Empty.ToString())
        {
            tabLinks.Add(Guid.Parse(ApplicantInfoSlotId), FlexConsts.ApplicantInfoUiAnchor);
        }

        if (CustomTabsSlotIds != null && CustomTabsSlotIds != Guid.Empty.ToString())
        {
            var customTabs = CustomTabsSlotIds.Split(';');
            foreach (var customTabId in customTabs)
            {
                tabLinks.Add(Guid.Parse(customTabId), FlexConsts.CustomTab);
            }
        }

        _ = await worksheetLinkAppService.UpdateWorksheetLinksAsync(FormVersionId, CorrelationConsts.FormVersion, new UpdateWorksheetLinksDto()
        {
            WorksheetAnchors = tabLinks
        });

        return new OkObjectResult(new { FormVersionId });
    }
}

