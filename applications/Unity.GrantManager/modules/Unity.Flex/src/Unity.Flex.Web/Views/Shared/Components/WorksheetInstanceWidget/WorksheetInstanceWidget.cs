using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.GrantManager.Flex;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget;

[Widget(
        RefreshUrl = "../Flex/Widgets/WorksheetInstance/Refresh",
        ScriptTypes = [typeof(WorksheetInstanceWidgetScriptBundleContributor)],
        StyleTypes = [typeof(WorksheetInstanceWidgetStyleBundleContributor)],
        AutoInitialize = true)]
public class WorksheetInstanceWidget(IWorksheetInstanceAppService worksheetInstanceAppService, IWorksheetAppService worksheetAppService) : AbpViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(Guid instanceCorrelationId,
        string instanceCorrelationProvider,
        Guid sheetCorrelationId,
        string sheetCorrelationProvider,
        string uiAnchor,
        Guid? worksheetId)
    {
        if (instanceCorrelationProvider == FlexConsts.Preview
            && sheetCorrelationProvider == FlexConsts.Preview
            && uiAnchor == FlexConsts.Preview
            && worksheetId != null)
        {
            return await RenderPreviewAsync(worksheetId.Value);
        }

        // Invalid render
        if (instanceCorrelationId == Guid.Empty
            && sheetCorrelationId == Guid.Empty) return View(new WorksheetViewModel());

        // Render instance or worksheet
        return await RenderViewAsync(instanceCorrelationId, instanceCorrelationProvider, sheetCorrelationId, sheetCorrelationProvider, uiAnchor, worksheetId);
    }

    private async Task<IViewComponentResult> RenderViewAsync(Guid instanceCorrelationId,
        string instanceCorrelationProvider,
        Guid sheetCorrelationId,
        string sheetCorrelationProvider,
        string uiAnchor,
        Guid? worksheetId)
    {
        WorksheetViewModel? viewModel = null;

        var worksheetInstance = await worksheetInstanceAppService.GetByCorrelationAnchorAsync(instanceCorrelationId,
            instanceCorrelationProvider,
            worksheetId,
            uiAnchor);

        WorksheetDto? worksheet;
        if (uiAnchor == FlexConsts.CustomTab)
        {
            if (worksheetId == null) return View(viewModel);
            worksheet = await worksheetAppService.GetAsync(worksheetId.Value);
        }
        else
        {
            worksheet = await worksheetAppService.GetByCorrelationAnchorAsync(sheetCorrelationId, sheetCorrelationProvider, uiAnchor);
        }

        if (worksheet == null) return View(new WorksheetViewModel());

        if (worksheetInstance == null)
        {
            viewModel = MapWorksheet(worksheet, uiAnchor);
        }
        else
        {
            viewModel = MapWorksheetInstance(worksheet, uiAnchor, worksheetInstance);
        }

        return View(viewModel);
    }

    private async Task<IViewComponentResult> RenderPreviewAsync(Guid worksheetId)
    {
        // make sure not deleted and was selected preview
        if (!await worksheetAppService.ExistsAsync(worksheetId))
        {
            return View(new WorksheetViewModel());
        }

        var worksheet = await worksheetAppService.GetAsync(worksheetId);
        WorksheetViewModel? viewModel = MapWorksheet(worksheet, "Preview");
        return View(viewModel);
    }

    private static WorksheetViewModel MapWorksheet(WorksheetDto worksheetDto, string uiAnchor)
    {
        var worksheetVM = new WorksheetViewModel
        {
            IsConfigured = true,
            Name = worksheetDto.Name,
            UiAnchor = uiAnchor,
            WorksheetId = worksheetDto.Id
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
                    CurrentValue = null,
                    UiAnchor = uiAnchor
                });
            }
        }

        return worksheetVM;
    }

    private static WorksheetViewModel MapWorksheetInstance(WorksheetDto worksheet, string uiAnchor, WorksheetInstanceDto worksheetInstance)
    {
        var worksheetViewModel = MapWorksheet(worksheet, uiAnchor);

        foreach (var field in worksheetViewModel.Sections.SelectMany(s => s.Fields))
        {
            var fieldValue = worksheetInstance.Values?.Find(s => s.CustomFieldId == field.Id)?.CurrentValue ?? "{}";
            field.CurrentValue = fieldValue;
            field.UiAnchor = uiAnchor;
        }

        return worksheetViewModel;
    }
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


