using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.DataGridWidget;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Values;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Web.Pages.Flex
{
    [RemoteService(false)]
    public class DataGridReadService(ICustomFieldAppService customFieldAppService,
        ICustomFieldValueAppService customFieldValueAppService) : ApplicationService
    {
        internal static Dictionary<string, string> ApplyPresentationFormat(
            Dictionary<string, string> keyValuePairs,
            List<Tuple<string, string, CustomFieldType>> keyValueTypes)
        {
            var formattedKeyValuePairs = new Dictionary<string, string>();

            foreach (var keyValue in keyValuePairs)
            {
                var key = keyValue.Key;
                var value = keyValue.Value;

                var typeTuple = keyValueTypes.Find(kvt => kvt.Item1 == key);
                if (typeTuple != null)
                {
                    var formattedValue = value.ApplyPresentationFormatting(typeTuple.Item3.ToString(), null);
                    formattedKeyValuePairs.Add(key, formattedValue);
                }
                else
                {
                    formattedKeyValuePairs.Add(key, value);
                }
            }

            return formattedKeyValuePairs;
        }

        internal async Task<List<WorksheetFieldViewModel>?> GetPropertiesAsync(RowInputData dataProps)
        {
            if (IsFirstRow(dataProps))
            {
                return await GetFirstRowAsync(dataProps);
            }

            if (AddNewRow(dataProps))
            {
                return await GetNewRowAsync(dataProps);
            }

            return await GetExistingRowAsync(dataProps);
        }

        private async Task<List<WorksheetFieldViewModel>> GetFirstRowAsync(RowInputData dataProps)
        {
            var datagridDefinition = await customFieldAppService.GetAsync(dataProps.FieldId);
            if (datagridDefinition == null) return [];

            return DataGridServiceUtils.ConvertDataGridValue(new DataGridValue(), datagridDefinition, 0, true);
        }

        private async Task<List<WorksheetFieldViewModel>> GetNewRowAsync(RowInputData dataProps)
        {
            if (dataProps.ValueId == null) throw new ArgumentNullException(nameof(dataProps));
            var customFieldValue = await customFieldValueAppService.GetAsync(dataProps.ValueId.Value);
            if (customFieldValue.CurrentValue == null) return [];

            var datagridDefinition = await customFieldAppService.GetAsync(dataProps.FieldId);
            if (datagridDefinition == null) return [];

            var dataGridValue = JsonSerializer.Deserialize<DataGridValue>(customFieldValue.CurrentValue ?? "{}");

            return DataGridServiceUtils.ConvertDataGridValue(dataGridValue, datagridDefinition, dataProps.Row, true);
        }

        private async Task<List<WorksheetFieldViewModel>> GetExistingRowAsync(RowInputData dataProps)
        {
            if (dataProps.ValueId == null) throw new ArgumentNullException(nameof(dataProps));
            var customFieldValue = await customFieldValueAppService.GetAsync(dataProps.ValueId.Value);
            if (customFieldValue.CurrentValue == null) return [];

            var datagridDefinition = await customFieldAppService.GetAsync(dataProps.FieldId);
            if (datagridDefinition == null) return [];

            var dataGridValue = JsonSerializer.Deserialize<DataGridValue>(customFieldValue.CurrentValue ?? "{}");

            return DataGridServiceUtils.ConvertDataGridValue(dataGridValue, datagridDefinition, dataProps.Row, false);
        }

        private static bool AddNewRow(RowInputData dataProps)
        {
            return dataProps.IsNew;
        }

        private static bool IsFirstRow(RowInputData dataProps)
        {
            return (dataProps.ValueId == null || dataProps.ValueId == Guid.Empty) && dataProps.IsNew;
        }
    }
}
