using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets.Values;
using Unity.Modules.Shared.Correlation;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Web.Pages.Flex
{
    [RemoteService(false)]
    public class DataGridWriteService(ICustomFieldAppService customFieldAppService,
        ICustomFieldValueAppService customFieldValueAppService,
        IWorksheetInstanceAppService worksheetInstanceAppService) : ApplicationService
    {
        internal async Task<WriteDataRowResponse> WriteRowAsync(RowInputData rowInputData)
        {
            rowInputData.WorksheetInstanceId = await ValidateWorksheetInstanceId(rowInputData);

            if (IsAddFirstRow(rowInputData))
            {
                return await AddFirstRowAsync(rowInputData);
            }
            else if (IsAddNewRow(rowInputData))
            {
                return await AddRowAsync(rowInputData);
            }
            else
            {
                return await UpdateRowAsync(rowInputData);
            }
        }

        private async Task<Guid> ValidateWorksheetInstanceId(RowInputData rowInputData)
        {
            if (rowInputData.WorksheetInstanceId == Guid.Empty)
            {
                // Make sure we dont have an existing instance via another field within the same form

                var worksheetInstance = await worksheetInstanceAppService
                    .GetByCorrelationAnchorAsync(rowInputData.ApplicationId, CorrelationConsts.Application, rowInputData.WorksheetId, rowInputData.UiAnchor);

                if (worksheetInstance != null)
                {
                    rowInputData.WorksheetInstanceId = worksheetInstance.Id;
                }
            }

            return rowInputData.WorksheetInstanceId;
        }

        private static bool IsAddFirstRow(RowInputData rowInputData)
        {
            return rowInputData.ValueId == null;
        }

        private static bool IsAddNewRow(RowInputData rowInputData)
        {
            return rowInputData.IsNew;
        }

        internal async Task<WriteDataRowResponse> AddFirstRowAsync(RowInputData rowInputData)
        {
            var matchedValues = await GenerateKeyValueTypesAsync(rowInputData.FieldId, rowInputData.KeyValuePairs);
            var firstRowData = CalculateFirstRow(matchedValues);
            var dataGridValue = JsonSerializer.Serialize(new DataGridValue(firstRowData));

            if (firstRowData != null)
            {
                if (NewWorksheetInstanceRequired(rowInputData))
                {
                    return await CreateFirstRowWithWorksheetInstanceAsync(rowInputData, matchedValues, dataGridValue);
                }
                else
                {
                    return await CreateFirstRowForExistingWorksheetInstanceAsync(rowInputData, matchedValues, dataGridValue);
                }
            }

            // Set the worksheet instance value through the manager
            await customFieldValueAppService.SyncWorksheetInstanceValueAsync(rowInputData.WorksheetInstanceId);

            return new WriteDataRowResponse()
            {
                IsNew = false,
                ValueId = rowInputData.ValueId ?? Guid.Empty,
                MappedValues = matchedValues,
                WorksheetInstanceId = rowInputData.WorksheetInstanceId,
                WorksheetId = rowInputData.WorksheetId,
                Row = 0
            };
        }

        private async Task<WriteDataRowResponse> CreateFirstRowForExistingWorksheetInstanceAsync(RowInputData rowInputData,
            List<Tuple<string, string, CustomFieldType>> matchedValues,
            string dataGridValue)
        {
            var newDataRow = new CustomFieldValueDto()
            {
                CurrentValue = dataGridValue,
                CustomFieldId = rowInputData.FieldId,
                Id = Guid.NewGuid(),
                WorksheetInstanceId = rowInputData.WorksheetInstanceId
            };

            await customFieldValueAppService.ExplicitAddAsync(newDataRow);

            // Set the worksheet instance value through the manager
            await customFieldValueAppService.SyncWorksheetInstanceValueAsync(rowInputData.WorksheetInstanceId);

            return new WriteDataRowResponse()
            {
                IsNew = true,
                ValueId = newDataRow.Id,
                MappedValues = matchedValues,
                WorksheetInstanceId = rowInputData.WorksheetInstanceId,
                WorksheetId = rowInputData.WorksheetId,
                Row = 0
            };
        }

        private async Task<WriteDataRowResponse> CreateFirstRowWithWorksheetInstanceAsync(
            RowInputData rowInputData,
            List<Tuple<string, string, CustomFieldType>> matchedValues,
            string dataGridValue)
        {
            var worksheetInstance = await worksheetInstanceAppService.CreateAsync(new CreateWorksheetInstanceDto()
            {
                CorrelationAnchor = rowInputData.UiAnchor,
                WorksheetId = rowInputData.WorksheetId,
                CorrelationProvider = CorrelationConsts.Application,
                CorrelationId = rowInputData.ApplicationId,
                SheetCorrelationId = rowInputData.FormVersionId,
                SheetCorrelationProvider = CorrelationConsts.FormVersion,
                CurrentValue = await CreateExplicitWorksheetInstanceValueAsync(rowInputData.FieldId, dataGridValue),
                ReportData = await GenerateReportDataForNewRowAsync(rowInputData.FieldId, dataGridValue)
            });

            var newDataRow = new CustomFieldValueDto()
            {
                CurrentValue = dataGridValue,
                CustomFieldId = rowInputData.FieldId,
                Id = Guid.NewGuid(),
                WorksheetInstanceId = worksheetInstance.Id,
            };

            await customFieldValueAppService.ExplicitAddAsync(newDataRow);

            return new WriteDataRowResponse()
            {
                IsNew = true,
                ValueId = newDataRow.Id,
                MappedValues = matchedValues,
                WorksheetInstanceId = worksheetInstance.Id,
                WorksheetId = rowInputData.WorksheetId,
                Row = 0
            };
        }

        private async Task<string> GenerateReportDataForNewRowAsync(Guid customFieldId, string dataGridValue)
        {
            // Retrieve field
            var field = await customFieldAppService.GetAsync(customFieldId);

            // Parse the input data and deserialize the "value" property
            JObject dataValue = JObject.Parse(dataGridValue);
            var rowsValue = JsonSerializer.Deserialize<DataGridRowsValue>(dataValue["value"]?.ToString() ?? string.Empty);

            if (rowsValue == null) return "{}";

            // Create a dictionary to store the resulting values
            var values = rowsValue.Rows
                .SelectMany(row => row.Cells, (row, cell) => new { row, cell })
                .ToDictionary(
                    x => $"{field.Key}-{x.cell.Key}",
                    x => new List<string> { x.cell.Value }
                );

            // Serialize the dictionary to a JSON string and return it
            return JsonSerializer.Serialize(values);
        }

        private async Task<string> CreateExplicitWorksheetInstanceValueAsync(Guid fieldId, string dataGridValue)
        {
            var field = await customFieldAppService.GetAsync(fieldId);
            var instanceCurrentValue = new WorksheetInstanceValue();
            instanceCurrentValue.Values.Add(new FieldInstanceValue(field.Key, dataGridValue));
            return JsonSerializer.Serialize(instanceCurrentValue);
        }

        private static bool NewWorksheetInstanceRequired(RowInputData rowInputData)
        {
            return rowInputData.WorksheetInstanceId == Guid.Empty;
        }

        private static DataGridRowsValue CalculateFirstRow(List<Tuple<string, string, CustomFieldType>> matchedValues)
        {
            var newCells = new List<DataGridRowCell>();

            foreach (var value in matchedValues)
            {
                newCells.Add(new DataGridRowCell()
                {
                    Key = value.Item1,
                    Value = value.Item2.ApplyStoreFormatting(value.Item3.ToString())
                });
            }

            return new DataGridRowsValue()
            {
                Rows =
                [
                    new DataGridRow()
                    {
                        Cells = newCells
                    }
                ]
            };
        }

        internal async Task<WriteDataRowResponse> AddRowAsync(RowInputData rowInputData)
        {
            if (rowInputData.ValueId == null) throw new ArgumentNullException(nameof(rowInputData));

            var validValueId = rowInputData.ValueId.Value;
            var matchedValues = await GenerateKeyValueTypesAsync(rowInputData.FieldId, rowInputData.KeyValuePairs);

            (string?, uint) addedRowDelta = await CalculateAddedRowDeltaAsync(validValueId, matchedValues);

            if (addedRowDelta.Item1 != null)
            {
                await customFieldValueAppService.ExplicitSetAsync(validValueId, addedRowDelta.Item1);
            }

            // Set the worksheet instance value through the manager
            await customFieldValueAppService.SyncWorksheetInstanceValueAsync(rowInputData.WorksheetInstanceId);

            return new WriteDataRowResponse()
            {
                IsNew = true,
                ValueId = rowInputData.ValueId ?? Guid.Empty,
                MappedValues = matchedValues,
                Row = addedRowDelta.Item2,
                WorksheetInstanceId = rowInputData.WorksheetInstanceId,
                WorksheetId = rowInputData.WorksheetId
            };
        }

        internal async Task<WriteDataRowResponse> UpdateRowAsync(RowInputData rowInputData)
        {
            if (rowInputData.ValueId == null) throw new ArgumentNullException(nameof(rowInputData));

            var validValueId = rowInputData.ValueId.Value;
            var matchedValues = await GenerateKeyValueTypesAsync(rowInputData.FieldId, rowInputData.KeyValuePairs);
            var calculatedDelta = await CalculateDeltaAsync(validValueId, rowInputData.Row, matchedValues);

            if (calculatedDelta != null)
            {
                await customFieldValueAppService.ExplicitSetAsync(validValueId, calculatedDelta);
            }

            // Set the worksheet instance value through the manager
            await customFieldValueAppService.SyncWorksheetInstanceValueAsync(rowInputData.WorksheetInstanceId);

            return new WriteDataRowResponse()
            {
                IsNew = false,
                ValueId = rowInputData.ValueId ?? Guid.Empty,
                MappedValues = matchedValues,
                WorksheetId = rowInputData.WorksheetId,
                WorksheetInstanceId = rowInputData.WorksheetInstanceId,
                Row = rowInputData.Row
            };
        }

        internal async Task<List<Tuple<string, string, CustomFieldType>>> GenerateKeyValueTypesAsync(Guid customFieldId, Dictionary<string, string>? keyValuePairs)
        {
            var result = new List<Tuple<string, string, CustomFieldType>>();
            var datagridDefinition = await customFieldAppService.GetAsync(customFieldId);
            var definition = JsonSerializer.Deserialize<DataGridDefinition>(datagridDefinition?.Definition ?? "{}");

            foreach (var keyValuePair in keyValuePairs ?? [])
            {
                result.Add(new Tuple<string, string, CustomFieldType>(keyValuePair.Key,
                    keyValuePair.Value,
                    DataGridServiceUtils.ResolveTypeColumnName(keyValuePair.Key, definition)));
            }

            return result;
        }

        internal async Task<string?> CalculateDeltaAsync(Guid valueId, uint row, List<Tuple<string, string, CustomFieldType>> keyValueTypes)
        {
            var currentValue = await customFieldValueAppService.GetAsync(valueId);
            var dataGridValue = DataGridServiceUtils.DeserializeDataGridValue(currentValue.CurrentValue?.ToString());
            if (dataGridValue == null) return null;

            var dataGridRowsValue = DataGridServiceUtils.DeserializeDataGridRowsValue(dataGridValue.Value?.ToString());
            if (dataGridRowsValue == null) return null;

            var rowToUpdate = dataGridRowsValue.Rows[(int)row];
            DataGridServiceUtils.UpdateRowCells(keyValueTypes, rowToUpdate);

            dataGridValue.Value = dataGridRowsValue;
            return JsonSerializer.Serialize(dataGridValue);
        }

        internal async Task<(string?, uint)> CalculateAddedRowDeltaAsync(Guid valueId, List<Tuple<string, string, CustomFieldType>> keyValueTypes)
        {
            var currentValue = await customFieldValueAppService.GetAsync(valueId);
            var dataGridValue = DataGridServiceUtils.DeserializeDataGridValue(currentValue.CurrentValue?.ToString());
            if (dataGridValue == null) return (null, 0);

            var dataGridRowsValue = DataGridServiceUtils.DeserializeDataGridRowsValue(dataGridValue.Value?.ToString());
            if (dataGridRowsValue == null) return (null, 0);

            dataGridRowsValue.Rows.Add(new DataGridRow()
            {
                Cells = DataGridServiceUtils.SetRowCells(keyValueTypes)
            });

            dataGridValue.Value = dataGridRowsValue;

            return (JsonSerializer.Serialize(dataGridValue), (uint)(dataGridRowsValue.Rows.Count - 1));
        }
    }
}
