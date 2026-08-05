using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.History;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.GrantManager.Web.Views.Shared.Components.HistoryWidget;

[Widget(
    RefreshUrl = "Widgets/History/RefreshHistory",
    ScriptTypes = [typeof(HistoryWidgetScriptBundleContributor)],
    StyleTypes = [typeof(HistoryWidgetStyleBundleContributor)],
    AutoInitialize = true)]
public class HistoryWidgetViewComponent(
    IApplicationStatusRepository applicationStatusRepository,
    IHistoryAppService historyAppService) : AbpViewComponent
{
    public const string Property_ApplicationStatusId = "ApplicationStatusId";
    public const string Property_ExternalStatusVisibility = "ExternalStatusVisibility";
    public async Task<IViewComponentResult> InvokeAsync(Guid applicationId)
    {
        string? entityId = applicationId.ToString();
        Dictionary<string, string> applicationStatusDict = await GetApplicationStatusDict();

        HistoryWidgetViewModel model = new()
        {
            ApplicationStatusHistoryList = await historyAppService.GetEntityPropertyChangesAsync(
            new GetEntityPropertyChangesInput
            {
                EntityId = entityId,
                PropertyNames = [Property_ApplicationStatusId, Property_ExternalStatusVisibility],
            }, applicationStatusDict),
        };

        return View(model);
    }

    private async Task<Dictionary<string, string>> GetApplicationStatusDict() {
        List<ApplicationStatus> applicationStatusList = await applicationStatusRepository.GetListAsync();
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


