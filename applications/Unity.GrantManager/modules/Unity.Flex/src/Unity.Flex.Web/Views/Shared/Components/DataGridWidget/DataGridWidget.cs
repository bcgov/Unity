using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Volo.Abp.AspNetCore.Mvc.UI.Widgets;
using Volo.Abp.AspNetCore.Mvc;
using System.Text.Json;
using Unity.Flex.Worksheets.Values;
using System.Linq;
using System.Collections.Generic;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using System.Globalization;
using System;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridWidget
{
    [ViewComponent(Name = "DataGridWidget")]
    [Widget(RefreshUrl = "../Flex/Widgets/DataGrid/Refresh",
        ScriptTypes = [typeof(DataGridWidgetScriptBundleContributor)],
        AutoInitialize = true)]
    public class DataGridWidget : AbpViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            var dataGridValue = JsonSerializer.Deserialize<DataGridValue>(fieldModel?.CurrentValue ?? "{}");
            var value = JsonSerializer.Deserialize<DataGridRowsValue>(dataGridValue.Value.ToString());

            var columns = dataGridValue?.Columns ?? [];
            List<DataGridViewModelRow> rows = [];

            foreach (var row in value.Rows)
            {
                List<DataGridViewModelCell> cells = [];
                foreach (var column in columns)
                {
                    var correctField = row.Cells.Find(s => s.Key == column.Key);
                    cells.Add(new DataGridViewModelCell()
                    {
                        Key = correctField.Key,
                        Value = correctField.Value.ApplyFormatting(column.Type, column.Format)
                    });
                }

                rows.Add(new DataGridViewModelRow()
                {
                    Cells = cells
                });
            }

            var dataColumns = dataGridValue?.Columns.ToArray() ?? [];

            var viewModel = await Task.FromResult(new DataGridViewModel()
            {
                Field = fieldModel,
                Name = modelName,
                Columns = dataColumns.Select(s => s.Name).ToArray() ?? [],
                Rows = rows.ToArray()
            });
            ;
            viewModel.Summary = GenerateSummary(dataColumns, viewModel.Rows);

            return View(viewModel);
        }

        private DataGridViewSummary GenerateSummary(DataGridColumn[]? dataColumns, DataGridViewModelRow[] rows)
        {
            var summary = new DataGridViewSummary();

            foreach (var field in dataColumns)
            {
                if (field.Type == "simplecurrencyadvanced")
                {
                    summary.Fields.Add(new DataGridViewModelSummaryField()
                    {
                        Key = field.Key,
                        Value = SumCells(field.Key, rows),
                        Label = field.Name
                    });
                }
            }

            return summary;
        }

        private string SumCells(string? key, DataGridViewModelRow[] rows)
        {
            decimal sum = 0;
            foreach (var row in rows)
            {
                var cell = row.Cells.Find(x => x.Key == key);
                sum += decimal.Parse(cell.Value.Replace("$", "").Replace(",", ""));
            }
            return sum.ToString();
        }
    }


    public class DataGridWidgetScriptBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/DataGridWidget/Default.js");
        }
    }

    public static class DataGridExtensions
    {
        public static string? ApplyFormatting(this string? value, string columnType, string format)
        {
            // "currency", "format"
            if (columnType == "simpledatetimeadvanced" && value != null && DateTime.TryParse(value, out DateTime dateTime))
            {
                var appliedFormat = !string.IsNullOrEmpty(format) ? format : "MM-dd-yyyy"; // Date vs DateTime                
                string formattedDateTime = dateTime.ToString(appliedFormat, CultureInfo.InvariantCulture);
                return formattedDateTime;
            }

            if (columnType == "simplecurrencyadvanced" && value != null && decimal.TryParse(value, out decimal number))
            {
                var currencyCode = !string.IsNullOrEmpty(format) ? format : "CAD";
                var culture = GetCultureInfoByCurrencyCode(currencyCode);
                string formattedNumber = number.ToString("C", culture);
                return formattedNumber;
            }
            return value;
        }
        static CultureInfo GetCultureInfoByCurrencyCode(string currencyCode)
        {
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                var region = new RegionInfo(culture.Name);
                if (region.ISOCurrencySymbol == currencyCode)
                {
                    return culture;
                }
            }

            throw new ArgumentException("Invalid or unsupported currency code.");
        }
    }
}
