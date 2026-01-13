using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Alerts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Flex;
using Unity.GrantManager.ApplicationForms;
using Unity.Flex.WorksheetLinks;
using Unity.Modules.Shared.Correlation;

namespace Unity.GrantManager.Web.Views.Shared.Components.CustomFields
{


    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/CustomFields")]
    public class CustomFieldsController(IWorksheetLinkAppService worksheetLinkAppService,

        IApplicationFormVersionAppService applicationFormVersionAppService) : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpPost("update")]
        public async Task<IActionResult> UpdateCustomFields([FromForm] CustomFieldsViewModel model)
        {
            var tabLinks = new List<(Guid worksheetId, string anchor, uint order)>();

            ProcessSlotIds(model.CustomTabsSlotIds, FlexConsts.CustomTab, tabLinks);
            ProcessSlotIds(model.AssessmentInfoSlotIds, FlexConsts.AssessmentInfoUiAnchor, tabLinks);
            ProcessSlotIds(model.ProjectInfoSlotIds, FlexConsts.ProjectInfoUiAnchor, tabLinks);
            ProcessSlotIds(model.ApplicantInfoSlotIds, FlexConsts.ApplicantInfoUiAnchor, tabLinks);
            ProcessSlotIds(model.PaymentInfoSlotIds, FlexConsts.PaymentInfoUiAnchor, tabLinks);
            ProcessSlotIds(model.FundingAgreementInfoSlotIds, FlexConsts.FundingAgreementInfoUiAnchor, tabLinks);

            var formVersion = await applicationFormVersionAppService.GetByChefsFormVersionId(model.ChefsFormVersionId);
            _ = await worksheetLinkAppService
                .UpdateWorksheetLinksAsync(new UpdateWorksheetLinksDto()
                {
                    CorrelationId = formVersion?.Id ?? Guid.Empty,
                    CorrelationProvider = CorrelationConsts.FormVersion,
                    WorksheetAnchors = tabLinks
                });

            Guid chefsFormVersionId = !string.IsNullOrEmpty(formVersion?.ChefsFormVersionGuid) 
                ? Guid.Parse(formVersion.ChefsFormVersionGuid) 
                : Guid.Empty;
        
            return new OkObjectResult(new { chefsFormVersionId });
        }

        private static void ProcessSlotIds(string? slotIds, string uiAnchor, List<(Guid worksheetId, string anchor, uint order)> tabLinks)
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
}
