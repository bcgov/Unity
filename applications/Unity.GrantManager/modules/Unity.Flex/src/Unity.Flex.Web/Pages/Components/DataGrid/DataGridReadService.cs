using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
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
            List<Tuple<string, string, CustomFieldType>> keyValueTypes,
            PresentationSettings presentationSettings)
        {
            var formattedKeyValuePairs = new Dictionary<string, string>();

            foreach (var keyValue in keyValuePairs)
            {
                var key = keyValue.Key;
                var value = keyValue.Value;

                var typeTuple = keyValueTypes.Find(kvt => kvt.Item1 == key);
                if (typeTuple != null)
                {
                    var formattedValue = value.ApplyPresentationFormatting(typeTuple.Item3.ToString(), null, presentationSettings);
                    formattedKeyValuePairs.Add(key, formattedValue);
                }
                else
                {
                    formattedKeyValuePairs.Add(key, value);
                }
            }

            return formattedKeyValuePairs;
        }

        internal async Task<(KeyValuePair<string, string>[] dynamicFields, List<WorksheetFieldViewModel>? customFields)> GetPropertiesAsync(RowInputData dataProps, PresentationSettings presentationSettings)
        {
            if (IsFirstRow(dataProps))
            {
                return ([], await GetFirstRowAsync(dataProps));
            }

            if (AddNewRow(dataProps))
            {
                return ([], await GetNewRowAsync(dataProps));
            }

            return await GetExistingRowAsync(dataProps, presentationSettings);
        }

        private async Task<List<WorksheetFieldViewModel>> GetFirstRowAsync(RowInputData dataProps)
        {
            var datagridDefinition = await customFieldAppService.GetAsync(dataProps.FieldId);
            if (datagridDefinition == null) return [];

            return DataGridServiceUtils.ExtractCustomColumnsValues(new DataGridValue(), datagridDefinition, 0, true);
        }

        private async Task<List<WorksheetFieldViewModel>> GetNewRowAsync(RowInputData dataProps)
        {
            if (dataProps.ValueId == null) throw new ArgumentNullException(nameof(dataProps));
            var customFieldValue = await customFieldValueAppService.GetAsync(dataProps.ValueId.Value);
            if (customFieldValue.CurrentValue == null) return [];

            var datagridDefinition = await customFieldAppService.GetAsync(dataProps.FieldId);
            if (datagridDefinition == null) return [];

            var dataGridValue = JsonSerializer.Deserialize<DataGridValue>(customFieldValue.CurrentValue ?? "{}");

            return DataGridServiceUtils.ExtractCustomColumnsValues(dataGridValue, datagridDefinition, dataProps.Row, true);
        }

        private async Task<(KeyValuePair<string, string>[] dynamicFields, List<WorksheetFieldViewModel> customFields)> GetExistingRowAsync(RowInputData dataProps, PresentationSettings presentationSettings)
        {
            if (dataProps.ValueId == null) throw new ArgumentNullException(nameof(dataProps));
            var customFieldValue = await customFieldValueAppService.GetAsync(dataProps.ValueId.Value);
            if (customFieldValue.CurrentValue == null) return ([], []);

            var datagridDefinition = await customFieldAppService.GetAsync(dataProps.FieldId);
            if (datagridDefinition == null) return ([], []);

            var dataGridValue = JsonSerializer.Deserialize<DataGridValue>(customFieldValue.CurrentValue ?? "{}");

            return (DataGridServiceUtils.ExtractDynamicColumnsPairs(dataGridValue, dataProps.Row, presentationSettings),
                DataGridServiceUtils.ExtractCustomColumnsValues(dataGridValue, datagridDefinition, dataProps.Row, false));
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
