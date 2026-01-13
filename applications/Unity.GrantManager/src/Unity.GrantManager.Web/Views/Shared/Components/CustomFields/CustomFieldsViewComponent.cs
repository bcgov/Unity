using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.ApplicationForms;
using Unity.Flex.Worksheets;
using Unity.Flex.WorksheetLinks;
using Unity.GrantManager.Flex;
using Unity.Modules.Shared.Correlation;
using Unity.Flex.Scoresheets;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.Web.Views.Shared.Components.CustomFields
{

    [Widget(
        RefreshUrl = "CustomFields/Refresh",
        ScriptTypes = new[] { typeof(CustomFieldsScriptBundleContributor) },
        StyleTypes = new[] { typeof(CustomFieldsStyleBundleContributor) },
        AutoInitialize = true)]
    public class CustomFieldsViewComponent(IApplicationFormVersionAppService applicationFormVersionAppService,
                                            IApplicationFormRepository applicationFormRepository,
                                            IScoresheetAppService scoresheetAppService,
                                            IWorksheetLinkAppService worksheetLinkAppService,
                                            IWorksheetListAppService worksheetListAppService) : AbpViewComponent
    {

        public string AccountCodeList { get; set; } = string.Empty;

        public async Task<IViewComponentResult> InvokeAsync(string? formVersionId, string formName)
        {
            var model = new CustomFieldsViewModel { };
            model.ChefsFormVersionId = Guid.Parse(formVersionId ?? Guid.Empty.ToString());
            model.FormName = formName;

            var formVersion = await applicationFormVersionAppService.GetByChefsFormVersionId(model.ChefsFormVersionId);
            model.Version = formVersion?.Version?.ToString();
            model.WorksheetLinks = await worksheetLinkAppService.GetListByCorrelationAsync(formVersion?.Id ?? Guid.Empty, CorrelationConsts.FormVersion);

            model.PublishedWorksheets = [.. (await worksheetListAppService.GetListAsync())
                .Where(s => s.Published && !model.WorksheetLinks.Select(s => s.WorksheetId).Contains(s.Id))
                .OrderBy(s => s.Title)];

            (model.CustomTabLinks, model.CustomTabsSlotIds) = ProcessWorksheetLinks(model.WorksheetLinks, FlexConsts.CustomTab);
            (model.AssessmentInfoLinks, model.AssessmentInfoSlotIds) = ProcessWorksheetLinks(model.WorksheetLinks, FlexConsts.AssessmentInfoUiAnchor);
            (model.ProjectInfoLinks, model.ProjectInfoSlotIds) = ProcessWorksheetLinks(model.WorksheetLinks, FlexConsts.ProjectInfoUiAnchor);
            (model.ApplicantInfoLinks, model.ApplicantInfoSlotIds) = ProcessWorksheetLinks(model.WorksheetLinks, FlexConsts.ApplicantInfoUiAnchor);
            (model.PaymentInfoLinks, model.PaymentInfoSlotIds) = ProcessWorksheetLinks(model.WorksheetLinks, FlexConsts.PaymentInfoUiAnchor);
            (model.FundingAgreementInfoLinks, model.FundingAgreementInfoSlotIds) = ProcessWorksheetLinks(model.WorksheetLinks, FlexConsts.FundingAgreementInfoUiAnchor);

            var applicationFormId = formVersion?.ApplicationFormId;
            var applicationForm = applicationFormId.HasValue 
                ? await applicationFormRepository.FindAsync(x => x.Id == applicationFormId.Value)
                : null;
            model.ScoresheetId = applicationForm?.ScoresheetId;

            var scoresheets = await scoresheetAppService.GetAllPublishedScoresheetsAsync();
            model.ScoresheetOptionsList = [];

            foreach (var scoresheet in scoresheets)
            {
                model.ScoresheetOptionsList.Add(new SelectListItem
                {
                    Text = $"{scoresheet.Title} ({scoresheet.Name})",
                    Value = scoresheet.Id.ToString()
                });
            }

            model.ScoresheetOptionsList = [.. model.ScoresheetOptionsList.OrderBy(item => item.Text)];

            return View(model);
        }


        private static (List<WorksheetLinkDto> links, string slotIds) ProcessWorksheetLinks(List<WorksheetLinkDto> worksheetLinks, string uiAnchor)
        {
            var links = worksheetLinks.Where(s => s.UiAnchor == uiAnchor).ToList();
            var slotIds = string.Join(";", links.OrderBy(s => s.Order).Select(s => s.WorksheetId));
            return (links, slotIds);
        }




    }

    public class CustomFieldsStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CustomFields/Default.css");

        }
    }

    public class CustomFieldsScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/CustomFields/Default.js");
            context.Files
              .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}
