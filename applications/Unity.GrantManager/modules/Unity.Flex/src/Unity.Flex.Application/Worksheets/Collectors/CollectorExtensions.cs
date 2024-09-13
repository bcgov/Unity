using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Worksheets.Collectors
{
    public static class CollectorExtensions
    {
        public async static Task<CustomValueBase> ApplyCollectorAsync(this CustomValueBase customValue, CustomFieldType type, IServiceProvider serviceProvider)
        {
            // Collect / Derive additional information about the data received
            return type switch
            {
                CustomFieldType.BCAddress => await new BCAddressCollector(serviceProvider).CollectAsync((BCAddressValue)customValue),
                _ => customValue
            };
        }

        public static bool RequiresCollection(this Worksheet worksheet)
        {
            // Right now only BC Address requires data collection
            // udpate / refactor when we extend inferred data types
            if (worksheet.Sections.SelectMany(s => s.Fields).Any(s => s.Type == CustomFieldType.BCAddress))
            {
                return true;
            }

            return false;
        }

        public static async Task CollectAsync(this WorksheetInstance worksheetInstance, Worksheet worksheet, IServiceProvider serviceProvider)
        {
            foreach (var field in worksheetInstance.Values)
            {
                var worksheetField = worksheet.Sections.SelectMany(s => s.Fields).FirstOrDefault(s => s.Id == field.CustomFieldId);
                if (worksheetField != null)
                {
                    switch (worksheetField.Type)
                    {
                        case CustomFieldType.BCAddress:
                            var value = JsonSerializer.Deserialize<BCAddressValue>(field.CurrentValue);
                            if (value != null)
                            {
                                field.SetValue(JsonSerializer.Serialize(await value.ApplyCollectorAsync(worksheetField.Type, serviceProvider)));
                            }
                            break;
                    }
                }
            }
        }
    }
}
