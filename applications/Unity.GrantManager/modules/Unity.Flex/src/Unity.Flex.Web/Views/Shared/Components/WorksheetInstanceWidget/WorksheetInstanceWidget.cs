using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets.Values;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.MultiTenancy;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget;

[ViewComponent(Name = "WorksheetInstanceWidget")]
[Widget(
        RefreshUrl = "Widgets/WorksheetInstance/Refresh",
        ScriptTypes = [typeof(WorksheetInstanceWidgetScriptBundleContributor)],
        StyleTypes = [typeof(WorksheetInstanceWidgetStyleBundleContributor)],
        AutoInitialize = true)]
public class WorksheetInstanceWidget(IWorksheetInstanceAppService worksheetInstanceAppService, IWorksheetAppService worksheetAppService, ICurrentTenant currentTenant) : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Guid correlationId, string correlationProvider, string uiAnchor)
    {
        WorksheetViewModel viewModel;

        var worksheetInstance = await worksheetInstanceAppService.GetByCorrelationAsync(correlationId, correlationProvider, uiAnchor);
        var worksheet = await worksheetAppService.GetByCorrelationAsync(currentTenant.Id ?? Guid.Empty, CorrelationConsts.Tenant, uiAnchor);

        if (worksheet == null) return View(new WorksheetViewModel());

        if (worksheetInstance == null)
        {
            viewModel = MapWorksheet(worksheet);
        }
        else
        {
            viewModel = MapWorksheetInstance(worksheet, worksheetInstance);
        }

        return View(viewModel);
    }

    private static WorksheetViewModel MapWorksheet(WorksheetDto worksheetDto)
    {
        var worksheetVM = new WorksheetViewModel
        {
            IsConfigured = true,
            UiAnchor = worksheetDto.UiAnchor,
            Name = worksheetDto.Name
        };

        foreach (var section in worksheetDto.Sections.OrderBy(s => s.Order))
        {
            worksheetVM.Sections.Add(new WorksheetInstanceSectionViewModel()
            {
                Name = section.Name
            });

            foreach (var field in section.Fields)
            {
                worksheetVM.Sections[^1].Fields.Add(new WorksheetFieldViewModel()
                {
                    Id = field.Id,
                    Name = field.Name,
                    Label = field.Label,
                    Definition = field.Definition,
                    Enabled = field.Enabled,
                    Order = field.Order,
                    Type = field.Type,
                    CurrentValue = null
                });
            }
        }

        return worksheetVM;
    }

    private static WorksheetViewModel MapWorksheetInstance(WorksheetDto worksheet, WorksheetInstanceDto worksheetInstance)
    {
        var worksheetViewModel = MapWorksheet(worksheet);

        foreach (var field in worksheetViewModel.Sections.SelectMany(s => s.Fields))
        {
            var fieldValue = worksheetInstance.Values?.Find(s => s.CustomFieldId == field.Id)?.CurrentValue ?? "{}";
            field.CurrentValue = fieldValue;
        }

        return worksheetViewModel;
    }

    public class WorksheetInstanceWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/WorksheetInstanceWidget/Default.css");
        }
    }

    public class WorksheetInstanceWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/WorksheetInstanceWidget/Default.js");
        }
    }
}


