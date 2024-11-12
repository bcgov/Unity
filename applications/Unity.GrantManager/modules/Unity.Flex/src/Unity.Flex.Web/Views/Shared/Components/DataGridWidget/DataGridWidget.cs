﻿using Microsoft.AspNetCore.Mvc;
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
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets;
using System.Text;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridWidget
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "TODO - invalid cast types when implemented")]
    [ViewComponent(Name = "DataGridWidget")]
    [Widget(RefreshUrl = "../Flex/Widgets/DataGrid/Refresh",
        ScriptTypes = [typeof(DataGridWidgetScriptBundleContributor)],
        StyleTypes = [typeof(DataGridWidgetStyleBundleContributor)],
        AutoInitialize = true)]
    public class DataGridWidget : AbpViewComponent
    {
        private const string _dynamicLabel = "Dynamic";
        private const string _summaryLabelprefix = "Total:";
        private static readonly List<CustomFieldType> _validTotalSummaryTypes = [CustomFieldType.Numeric, CustomFieldType.Currency];

        public IViewComponentResult Invoke(WorksheetFieldViewModel? fieldModel, string modelName)
        {
            if (fieldModel == null) return View(new DataGridViewModel());
            var dataGridValue = JsonSerializer.Deserialize<DataGridValue>(fieldModel.CurrentValue ?? "{}");
            var dataGridDefinition = (DataGridDefinition?)fieldModel.Definition?.ConvertDefinition(fieldModel.Type);

            // No definition, so nothing to display
            if (dataGridDefinition == null) return View(new DataGridViewModel() { Field = fieldModel });


            if (dataGridValue?.Value == null && fieldModel.UiAnchor == "Preview")
            {
                return GeneratePreview(fieldModel, dataGridDefinition);
            }
            else
            {
                return GenerateView(fieldModel, modelName, dataGridDefinition);
            }
        }

        private static bool IsDynamicWithNoColumns(DataGridDefinition dataGridDefinition)
        {
            return dataGridDefinition.Dynamic && dataGridDefinition.Columns.Count == 0;
        }

        private static bool IsDynamicWithColumns(DataGridDefinition dataGridDefinition)
        {
            return dataGridDefinition.Dynamic && dataGridDefinition.Columns.Count > 0;
        }

        private static bool IsNotDynamicWithColumns(DataGridDefinition dataGridDefinition)
        {
            return !dataGridDefinition.Dynamic && dataGridDefinition.Columns.Count > 0;
        }

        private IViewComponentResult GenerateView(WorksheetFieldViewModel fieldModel, string modelName, DataGridDefinition dataGridDefinition)
        {
            var dataGridValue = JsonSerializer.Deserialize<DataGridValue>(fieldModel.CurrentValue ?? "{}");
            DataGridRowsValue? dataGridRowsValue = null;

            if (dataGridValue != null && dataGridValue.Value != null && dataGridValue.Value.ToString() != null)
            {
                dataGridRowsValue = JsonSerializer.Deserialize<DataGridRowsValue>(dataGridValue.Value.ToString() ?? string.Empty);
            }

            return GenerateGridView(fieldModel, modelName, dataGridValue, dataGridRowsValue, dataGridDefinition);
        }

        private IViewComponentResult GenerateGridView(WorksheetFieldViewModel fieldModel,
            string modelName,
            DataGridValue? dataGridValue,
            DataGridRowsValue? dataGridRowsValue,
            DataGridDefinition dataGridDefinition)
        {
            var dataColumns = GenerateDataColumns(dataGridValue, dataGridDefinition);
            var dataRows = GenerateDataRows(dataColumns, dataGridRowsValue);
            var columnNames = dataColumns.Select(s => s.Name);

            var viewModel = new DataGridViewModel()
            {
                Field = fieldModel,
                Name = modelName,
                Columns = [.. columnNames],
                Rows = [.. dataRows],
                AllowEdit = true,
                SummaryOption = ConvertSummaryOption(dataGridDefinition),
                Summary = GenerateSummary([.. dataColumns], [.. dataRows]),
                TableOptions = GenerateAvailableTableOptions(!dataGridDefinition.Dynamic)
            };

            return View(viewModel);
        }

        private static string GenerateAvailableTableOptions(bool allowAdd)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("ExportData");
            if (allowAdd)
            {
                stringBuilder.Append(",AddRecord");
            }
            return stringBuilder.ToString();
        }

        private static List<DataGridColumn> GenerateDataColumns(DataGridValue? dataGridValue, DataGridDefinition dataGridDefinition)
        {
            // Predefined column definitions
            List<DataGridColumn> dataColumns = dataGridValue?.Columns ?? [];
            foreach (var dataColumn in dataGridDefinition?.Columns ?? [])
            {
                dataColumns.Add(new DataGridColumn()
                {
                    Key = dataColumn.Name,
                    Name = dataColumn.Name,
                    Type = dataColumn.Type
                });
            }
            return dataColumns;
        }

        private static List<DataGridViewModelRow> GenerateDataRows(List<DataGridColumn> columns, DataGridRowsValue? dataGridRowsValue)
        {
            if (dataGridRowsValue == null) return [];
            List<DataGridViewModelRow> rows = [];

            foreach (var row in dataGridRowsValue.Rows)
            {
                List<DataGridViewModelCell> cells = [];

                foreach (var column in columns)
                {
                    var fieldMatch = row.Cells.Find(s => s.Key == column.Key);
                    if (fieldMatch != null)
                    {
                        cells.Add(new DataGridViewModelCell()
                        {
                            Key = fieldMatch.Key,
                            Value = fieldMatch.Value.ApplyFormatting(column.Type, column.Format)
                        });
                    }
                    else
                    {
                        cells.Add(new DataGridViewModelCell()
                        {
                            Key = column.Key,
                            Value = string.Empty
                        });
                    }
                }

                rows.Add(new DataGridViewModelRow()
                {
                    Cells = cells
                });
            }

            return rows;
        }

        private IViewComponentResult GeneratePreview(WorksheetFieldViewModel fieldModel, DataGridDefinition dataGridDefinition)
        {
            if (IsDynamicWithNoColumns(dataGridDefinition))
            {
                return GenerateDynamicWithNoColumnsPreview(fieldModel, dataGridDefinition);
            }

            if (IsDynamicWithColumns(dataGridDefinition))
            {
                return GenerateDynamicWithColumnsPreview(fieldModel, dataGridDefinition);
            }

            if (IsNotDynamicWithColumns(dataGridDefinition))
            {
                return GenerateNonDynamicWithColumnsPreview(fieldModel, dataGridDefinition);
            }

            return View(new DataGridViewModel());
        }

        private IViewComponentResult GenerateNonDynamicWithColumnsPreview(WorksheetFieldViewModel fieldModel, DataGridDefinition dataGridDefinition)
        {
            var summary = GenerateEmptySummary(dataGridDefinition.Columns, dataGridDefinition.Dynamic);

            var configuredColumns = dataGridDefinition.Columns.Select(s => s.Name).ToList();
            var columnsToRender = configuredColumns;

            var previewRows = GeneratePreviewRows(dataGridDefinition.Columns, dataGridDefinition.Dynamic);

            return View(new DataGridViewModel()
            {
                Columns = [.. columnsToRender],
                Summary = summary,
                Rows = [.. previewRows],
                SummaryOption = ConvertSummaryOption(dataGridDefinition),
                Field = fieldModel,
                AllowEdit = true,
                TableOptions = GenerateAvailableTableOptions(true)
            });
        }

        private IViewComponentResult GenerateDynamicWithColumnsPreview(WorksheetFieldViewModel fieldModel, DataGridDefinition dataGridDefinition)
        {
            var summary = GenerateEmptySummary(dataGridDefinition.Columns, dataGridDefinition.Dynamic);

            var columnsToRender = GenerateDynamicPlaceholderColumn();
            var configuredColumns = dataGridDefinition.Columns.Select(s => s.Name).ToList();
            columnsToRender.AddRange(configuredColumns);

            var previewRows = GeneratePreviewRows(dataGridDefinition.Columns, dataGridDefinition.Dynamic);

            return View(new DataGridViewModel()
            {
                Columns = [.. columnsToRender],
                Summary = summary,
                Rows = [.. previewRows],
                SummaryOption = ConvertSummaryOption(dataGridDefinition),
                Field = fieldModel,
                AllowEdit = true,
                TableOptions = GenerateAvailableTableOptions(false)
            });
        }

        private IViewComponentResult GenerateDynamicWithNoColumnsPreview(WorksheetFieldViewModel fieldModel, DataGridDefinition dataGridDefinition)
        {
            return View(new DataGridViewModel()
            {
                Columns = [.. GenerateDynamicPlaceholderColumn()],
                Rows = [.. GenerateDynamicRowPlaceholder()],
                Summary = GenerateDynamicPlaceholderSummary(),
                SummaryOption = ConvertSummaryOption(dataGridDefinition),
                Field = fieldModel,
                AllowEdit = false,
                TableOptions = GenerateAvailableTableOptions(false)
            });
        }

        private static DataGridViewSummary GenerateEmptySummary(List<DataGridDefinitionColumn> columns, bool dynamic)
        {
            var summary = new DataGridViewSummary();

            if (dynamic)
            {
                summary = GenerateDynamicPlaceholderSummary();
            }

            foreach (var column in columns.Where(s => IsTotalPossibleType(s.Type)))
            {
                summary.Fields.Add(new DataGridViewModelSummaryField()
                {
                    Key = column.Name,
                    Label = $"{_summaryLabelprefix} {column.Name}",
                    Value = column.Type
                });
            }

            return summary;
        }

        private static bool IsTotalPossibleType(string type)
        {
            var validType = Enum.TryParse(type, out CustomFieldType customFieldType);
            if (!validType) return false;
            if (_validTotalSummaryTypes.Contains(customFieldType)) { return true; }
            return false;
        }

        private static List<DataGridViewModelRow> GeneratePreviewRows(List<DataGridDefinitionColumn> columnsToRender, bool includeDynamic)
        {
            var cells = new List<DataGridViewModelCell>();
            if (includeDynamic)
            {
                cells.Add(new DataGridViewModelCell() { Key = _dynamicLabel, Value = _dynamicLabel });
            }
            foreach (var column in columnsToRender)
            {
                cells.Add(new DataGridViewModelCell() { Key = column.Name, Value = column.Type });
            }
            return
            [
                new()
                {
                    Cells = cells,
                    Id = Guid.NewGuid().ToString()
                }
            ];
        }

        private static List<DataGridViewModelRow> GenerateDynamicRowPlaceholder()
        {
            return
            [
                new()
                    {
                        Cells = [new DataGridViewModelCell() { Key = _dynamicLabel, Value = _dynamicLabel }],
                        Id = Guid.NewGuid().ToString()
                    }
            ];
        }

        private static DataGridDefinitionSummaryOption ConvertSummaryOption(DataGridDefinition dataGridDefinition)
        {
            return (DataGridDefinitionSummaryOption)Enum.Parse(typeof(DataGridDefinitionSummaryOption), dataGridDefinition.SummaryOption);
        }

        private static DataGridViewSummary GenerateDynamicPlaceholderSummary()
        {
            return new DataGridViewSummary()
            {
                Fields =
                [
                    new()
                    {
                        Key = _dynamicLabel,
                        Label = $"{_summaryLabelprefix} {_dynamicLabel}",
                        Value = _dynamicLabel
                    }
                ]
            };
        }

        private static List<string> GenerateDynamicPlaceholderColumn()
        {
            return [_dynamicLabel];
        }

        private DataGridViewSummary GenerateSummary(DataGridColumn[]? dataColumns, DataGridViewModelRow[] rows)
        {
            var summary = new DataGridViewSummary();

            foreach (var field in dataColumns?.Where(s => IsTotalPossibleType(s.Type)) ?? [])
            {
                summary.Fields.Add(new DataGridViewModelSummaryField()
                {
                    Key = field.Key,
                    Value = SumCells(field.Key, rows),
                    Label = $"{_summaryLabelprefix} {field.Name}"
                });
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

    public class DataGridWidgetStyleBundleContributor : BundleContributor
    {
        public override void ConfigureBundle(BundleConfigurationContext context)
        {
            context.Files
              .AddIfNotContains("/Views/Shared/Components/DataGridWidget/Default.css");
        }
    }

    public static class DataGridExtensions
    {
        public static string ApplyFormatting(this string value, string columnType, string? format)
        {
            if (format == null) { return value; }

            // We can format date fields based on a format
            if ((columnType == CustomFieldType.Date.ToString() || columnType == CustomFieldType.DateTime.ToString())
                && value != null
                && DateTime.TryParse(value, new CultureInfo("en-CA"), out DateTime dateTime))
            {
                var appliedFormat = !string.IsNullOrEmpty(format) ? format : "MM-dd-yyyy"; // Date vs DateTime?                
                string formattedDateTime = dateTime.ToString(appliedFormat, CultureInfo.InvariantCulture);
                return formattedDateTime;
            }

            // We format currency fields
            if (columnType == "Currency" && value != null && decimal.TryParse(value, out decimal number))
            {
                var currencyCode = !string.IsNullOrEmpty(format) ? format : "CAD";
                var culture = GetCultureInfoByCurrencyCode(currencyCode);
                string formattedNumber = number.ToString("C", culture);
                return formattedNumber;
            }

            return value ?? string.Empty;
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
