using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Unity.GrantManager.Applications;
using Unity.GrantManager.History;

namespace Unity.GrantManager.Web.Views.Shared.Components.HistoryWidget
{
    [Widget(
        RefreshUrl = "Widgets/History/RefreshHistory",
        ScriptTypes = [typeof(HistoryWidgetScriptBundleContributor)],
        StyleTypes = [typeof(HistoryWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class HistoryWidgetViewComponent : AbpViewComponent
    {  
        private readonly IApplicationStatusRepository _applicationStatusRepository;
        private readonly IHistoryAppService _historyAppService;

        public HistoryWidgetViewComponent(IApplicationStatusRepository applicationStatusRepository,
                                          IHistoryAppService historyAppService)
        {

            _applicationStatusRepository = applicationStatusRepository;
            _historyAppService = historyAppService;
        }

        public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
        {
            string? entityId = applicationId.ToString();
            Dictionary<string, string> applicationStatusDict = await GetApplicationStatusDict();

            HistoryWidgetViewModel model = new()
            {       
                ApplicationStatusHistoryList = await _historyAppService.GetHistoryList(entityId, "ApplicationStatusId", applicationStatusDict),
            };                 

            return View(model);
        }

        public async Task<Dictionary<string, string>> GetApplicationStatusDict() {
            List<ApplicationStatus> applicationStatusList = await _applicationStatusRepository.GetListAsync();
            Dictionary<string, string> applicationStatusDict = new Dictionary<string, string>();
            foreach (var applicationStatus in applicationStatusList)
            {
                applicationStatusDict.Add(applicationStatus.Id.ToString(), applicationStatus.InternalStatus);
            }
            return applicationStatusDict;
        }
    }

    public class HistoryWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
            .AddIfNotContains("/Views/Shared/Components/HistoryWidget/Default.css");
        }
    }

    public class HistoryWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
            .AddIfNotContains("/Views/Shared/Components/HistoryWidget/Default.js");
            context.Files
            .AddIfNotContains("/libs/pubsub-js/src/pubsub.js");
        }
    }
}


