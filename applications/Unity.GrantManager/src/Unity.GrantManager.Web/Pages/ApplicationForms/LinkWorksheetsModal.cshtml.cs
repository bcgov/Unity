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
    public string? AssessmentInfoSlotIds { get; set; }

    [BindProperty]
    public string? ProjectInfoSlotIds { get; set; }

    [BindProperty]
    public string? ApplicantInfoSlotIds { get; set; }

    [BindProperty]
    public string? PaymentInfoSlotIds { get; set; }

    [BindProperty]
    public string? FundingAgreementInfoSlotIds { get; set; }

    [BindProperty]
    public string? CustomTabsSlotIds { get; set; }

    [BindProperty]
    public List<WorksheetLinkDto>? WorksheetLinks { get; set; }

    [BindProperty]
    public List<WorksheetBasicDto>? PublishedWorksheets { get; set; }

    [BindProperty]
    public List<WorksheetLinkDto>? AssessmentInfoLinks { get; set; }

    [BindProperty]
    public List<WorksheetLinkDto>? ApplicantInfoLinks { get; set; }

    [BindProperty]
    public List<WorksheetLinkDto>? ProjectInfoLinks { get; set; }

    [BindProperty]
    public List<WorksheetLinkDto>? PaymentInfoLinks { get; set; }

    [BindProperty]
    public List<WorksheetLinkDto>? FundingAgreementInfoLinks { get; set; }

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


        (CustomTabLinks, CustomTabsSlotIds) = ProcessWorksheetLinks(WorksheetLinks, FlexConsts.CustomTab);
        (AssessmentInfoLinks, AssessmentInfoSlotIds) = ProcessWorksheetLinks(WorksheetLinks, FlexConsts.AssessmentInfoUiAnchor);
        (ProjectInfoLinks, ProjectInfoSlotIds) = ProcessWorksheetLinks(WorksheetLinks, FlexConsts.ProjectInfoUiAnchor);
        (ApplicantInfoLinks, ApplicantInfoSlotIds) = ProcessWorksheetLinks(WorksheetLinks, FlexConsts.ApplicantInfoUiAnchor);
        (PaymentInfoLinks, PaymentInfoSlotIds) = ProcessWorksheetLinks(WorksheetLinks, FlexConsts.PaymentInfoUiAnchor);
        (FundingAgreementInfoLinks, FundingAgreementInfoSlotIds) = ProcessWorksheetLinks(WorksheetLinks, FlexConsts.FundingAgreementInfoUiAnchor);
    }


    public async Task<IActionResult> OnPostAsync()
    {
        var tabLinks = new List<(Guid worksheetId, string anchor, uint order)>();


        ProcessSlotIds(CustomTabsSlotIds, FlexConsts.CustomTab, tabLinks);
        ProcessSlotIds(AssessmentInfoSlotIds, FlexConsts.AssessmentInfoUiAnchor, tabLinks);
        ProcessSlotIds(ProjectInfoSlotIds, FlexConsts.ProjectInfoUiAnchor, tabLinks);
        ProcessSlotIds(ApplicantInfoSlotIds, FlexConsts.ApplicantInfoUiAnchor, tabLinks);
        ProcessSlotIds(PaymentInfoSlotIds, FlexConsts.PaymentInfoUiAnchor, tabLinks);
        ProcessSlotIds(FundingAgreementInfoSlotIds, FlexConsts.FundingAgreementInfoUiAnchor, tabLinks);

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

    private (List<WorksheetLinkDto> links, string slotIds) ProcessWorksheetLinks(List<WorksheetLinkDto> worksheetLinks, string uiAnchor)
    {
        var links = worksheetLinks.Where(s => s.UiAnchor == uiAnchor).ToList();
        var slotIds = string.Join(";", links.OrderBy(s => s.Order).Select(s => s.WorksheetId));
        return (links, slotIds);
    }

    private void ProcessSlotIds(string? slotIds, string uiAnchor, List<(Guid worksheetId, string anchor, uint order)> tabLinks)
    {
        if (!string.IsNullOrWhiteSpace(slotIds) && slotIds != Guid.Empty.ToString())
        {
            var tabs = slotIds.Split(';');
            uint order = 1;
            foreach (var tabId in tabs)
            {
                tabLinks.Add(new(Guid.Parse(tabId), uiAnchor, order));
                order++;
            }
        }
    }
}

