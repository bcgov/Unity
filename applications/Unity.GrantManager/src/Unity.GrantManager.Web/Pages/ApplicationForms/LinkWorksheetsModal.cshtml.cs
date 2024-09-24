using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.WorksheetLinks;
using Unity.Flex.Worksheets;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Flex;
using Unity.Modules.Shared.Correlation;

namespace Unity.GrantManager.Web.Pages.ApplicationForms;

public class LinkWorksheetModalModel(IWorksheetListAppService worksheetListAppService,
    IWorksheetLinkAppService worksheetLinkAppService,
    IApplicationFormVersionAppService applicationFormVersionAppService) : GrantManagerPageModel
{
    [BindProperty]
    public Guid ChefsFormVersionId { get; set; }

    [BindProperty]
    public string? FormName { get; set; }

    [BindProperty]
    public string? AssessmentInfoSlotId { get; set; }

    [BindProperty]
    public string? ProjectInfoSlotId { get; set; }

    [BindProperty]
    public string? ApplicantInfoSlotId { get; set; }

    [BindProperty]
    public string? PaymentInfoSlotId { get; set; }

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
    public WorksheetLinkDto? PaymentInfoLink { get; set; }

    [BindProperty]
    public List<WorksheetLinkDto>? CustomTabLinks { get; set; }

    public async Task OnGetAsync(Guid formVersionId, string formName)
    {
        ChefsFormVersionId = formVersionId;
        FormName = formName;

        var formVersion = await applicationFormVersionAppService.GetByChefsFormVersionId(formVersionId);
        WorksheetLinks = await worksheetLinkAppService.GetListByCorrelationAsync(formVersion?.Id ?? Guid.Empty, CorrelationConsts.FormVersion);

        PublishedWorksheets = [.. (await worksheetListAppService.GetListAsync())
            .Where(s => s.Published && !WorksheetLinks.Select(s => s.WorksheetId).Contains(s.Id))
            .OrderBy(s => s.Title)];

        GetSlotIdAnchors(WorksheetLinks);

        CustomTabLinks = WorksheetLinks.Where(s => s.UiAnchor == FlexConsts.CustomTab).ToList();
        CustomTabsSlotIds = string.Join(";", CustomTabLinks.Select(s => s.Id));
    }

    private void GetSlotIdAnchors(List<WorksheetLinkDto> worksheetLinks)
    {
        AssessmentInfoLink = worksheetLinks.Find(s => s.UiAnchor == FlexConsts.AssessmentInfoUiAnchor);
        AssessmentInfoSlotId = AssessmentInfoLink?.WorksheetId.ToString();

        ProjectInfoLink = worksheetLinks.Find(s => s.UiAnchor == FlexConsts.ProjectInfoUiAnchor);
        ProjectInfoSlotId = ProjectInfoLink?.WorksheetId.ToString();

        ApplicantInfoLink = worksheetLinks.Find(s => s.UiAnchor == FlexConsts.ApplicantInfoUiAnchor);
        ApplicantInfoSlotId = ApplicantInfoLink?.WorksheetId.ToString();

        PaymentInfoLink = worksheetLinks.Find(s => s.UiAnchor == FlexConsts.PaymentInfoUiAnchor);
        PaymentInfoSlotId = PaymentInfoLink?.WorksheetId.ToString();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var tabLinks = new Dictionary<Guid, string>();

        AddSlotIdAnchors(tabLinks);

        if (CustomTabsSlotIds != null && CustomTabsSlotIds != Guid.Empty.ToString())
        {
            var customTabs = CustomTabsSlotIds.Split(';');
            foreach (var customTabId in customTabs)
            {
                tabLinks.Add(Guid.Parse(customTabId), FlexConsts.CustomTab);
            }
        }

        var formVersion = await applicationFormVersionAppService.GetByChefsFormVersionId(ChefsFormVersionId);
        _ = await worksheetLinkAppService
            .UpdateWorksheetLinksAsync(new UpdateWorksheetLinksDto()
            {
                CorrelationId = formVersion?.Id ?? Guid.Empty,
                CorrelationProvider = CorrelationConsts.FormVersion,
                WorksheetAnchors = tabLinks
            });

        return new OkObjectResult(new { ChefsFormVersionId });
    }

    private void AddSlotIdAnchors(Dictionary<Guid, string> tabLinks)
    {
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

        if (PaymentInfoSlotId != null && PaymentInfoSlotId != Guid.Empty.ToString())
        {
            tabLinks.Add(Guid.Parse(PaymentInfoSlotId), FlexConsts.PaymentInfoUiAnchor);
        }
    }
}

