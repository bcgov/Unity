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
    public string? FundingAgreementInfoSlotId { get; set; }

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
    public WorksheetLinkDto? FundingAgreementInfoLink { get; set; }

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
        CustomTabsSlotIds = string.Join(";", CustomTabLinks
            .OrderBy(s => s.Order)
            .Select(s => s.WorksheetId));

        AssessmentInfoLinks = WorksheetLinks.Where(s => s.UiAnchor == FlexConsts.AssessmentInfoUiAnchor).ToList();
        AssessmentInfoSlotIds = string.Join(";", AssessmentInfoLinks
            .OrderBy(s => s.Order)
            .Select(s => s.WorksheetId));

        ProjectInfoLinks = WorksheetLinks.Where(s => s.UiAnchor == FlexConsts.ProjectInfoUiAnchor).ToList();
        ProjectInfoSlotIds = string.Join(";", ProjectInfoLinks
            .OrderBy(s => s.Order)
            .Select(s => s.WorksheetId));

        ApplicantInfoLinks = WorksheetLinks.Where(s => s.UiAnchor == FlexConsts.ApplicantInfoUiAnchor).ToList();
        ApplicantInfoSlotIds = string.Join(";", ApplicantInfoLinks
            .OrderBy(s => s.Order)
            .Select(s => s.WorksheetId));

        PaymentInfoLinks = WorksheetLinks.Where(s => s.UiAnchor == FlexConsts.PaymentInfoUiAnchor).ToList();
        PaymentInfoSlotIds = string.Join(";", PaymentInfoLinks
            .OrderBy(s => s.Order)
            .Select(s => s.WorksheetId));
    }

    private void GetSlotIdAnchors(List<WorksheetLinkDto> worksheetLinks)
    {
        FundingAgreementInfoLink = worksheetLinks.Find(s => s.UiAnchor == FlexConsts.FundingAgreementInfoUiAnchor);
        FundingAgreementInfoSlotId = FundingAgreementInfoLink?.WorksheetId.ToString();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var tabLinks = new List<(Guid worksheetId, string anchor, uint order)>();

        AddSlotIdAnchors(tabLinks);

        if (CustomTabsSlotIds != null && CustomTabsSlotIds != Guid.Empty.ToString())
        {
            var customTabs = CustomTabsSlotIds.Split(';');
            uint order = 1;

            foreach (var customTabId in customTabs) //this comes in sequenced as we want for custom tabs
            {
                tabLinks.Add(new(Guid.Parse(customTabId), FlexConsts.CustomTab, order));
                order++;
            }
        }

        if (AssessmentInfoSlotIds != null && AssessmentInfoSlotIds != Guid.Empty.ToString())
        {
            var assessmentInfoTabs = AssessmentInfoSlotIds.Split(';');
            uint order = 1;

            foreach (var assessmentInfoTabId in assessmentInfoTabs) //this comes in sequenced as we want for assessment info tabs
            {
                tabLinks.Add(new(Guid.Parse(assessmentInfoTabId), FlexConsts.AssessmentInfoUiAnchor, order));
                order++;
            }
        }

        if (ProjectInfoSlotIds != null && ProjectInfoSlotIds != Guid.Empty.ToString())
        {
            var projectInfoTabs = ProjectInfoSlotIds.Split(';');
            uint order = 1;

            foreach (var projectInfoTabId in projectInfoTabs) //this comes in sequenced as we want for project info tabs
            {
                tabLinks.Add(new(Guid.Parse(projectInfoTabId), FlexConsts.ProjectInfoUiAnchor, order));
                order++;
            }
        }

        if (ApplicantInfoSlotIds != null && ApplicantInfoSlotIds != Guid.Empty.ToString())
        {
            var applicantInfoTabs = ApplicantInfoSlotIds.Split(';');
            uint order = 1;

            foreach (var applicantInfoTabId in applicantInfoTabs) //this comes in sequenced as we want for applicant info tabs
            {
                tabLinks.Add(new(Guid.Parse(applicantInfoTabId), FlexConsts.ApplicantInfoUiAnchor, order));
                order++;
            }
        }

        if (PaymentInfoSlotIds != null && PaymentInfoSlotIds != Guid.Empty.ToString())
        {
            var paymentInfoTabs = PaymentInfoSlotIds.Split(';');
            uint order = 1;

            foreach (var paymentInfoTabId in paymentInfoTabs) //this comes in sequenced as we want for payment info tabs
            {
                tabLinks.Add(new(Guid.Parse(paymentInfoTabId), FlexConsts.PaymentInfoUiAnchor, order));
                order++;
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

    private void AddSlotIdAnchors(List<(Guid worksheetId, string anchor, uint order)> tabLinks)
    {
        // We leave the order for the predefined tabs as 0 as they slot into a fixed position
        if (FundingAgreementInfoSlotId != null && FundingAgreementInfoSlotId != Guid.Empty.ToString())
        {
            tabLinks.Add(new(Guid.Parse(FundingAgreementInfoSlotId), FlexConsts.FundingAgreementInfoUiAnchor, 0));
        }
    }
}

