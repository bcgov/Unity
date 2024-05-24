using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets.Values;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Services;

namespace Unity.Flex.Domain.Services
{
    public class WorksheetsManager(IWorksheetInstanceRepository worksheetInstanceRepository, IWorksheetRepository worksheetRepository) : DomainService
    {
        public async Task PersistWorksheetData(PersistWorksheetIntanceValuesEto eventData)
        {
            string json = JsonSerializer.Serialize(eventData.CustomFields);
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (dictionary == null || dictionary.Count == 0) return;
            var fields = BuildFields(dictionary);

            var worksheetInstance = await worksheetInstanceRepository.GetByCorrelationAsync(eventData.CorrelationId, eventData.CorrelationProvider, eventData.UiAnchor, true);
            var worksheet = await worksheetRepository.GetByCorrelationAsync(CurrentTenant.Id ?? Guid.Empty, CorrelationConsts.Tenant, eventData.UiAnchor, true);

            if (worksheetInstance == null)
            {
                if (worksheet == null) return;

                var newInstance = new WorksheetInstance(Guid.NewGuid(),
                      worksheet.Id,
                      eventData.CorrelationId,
                      eventData.CorrelationProvider,
                      eventData.UiAnchor);

                foreach (var field in fields)
                {
                    var customField = FindCustomFieldByName(worksheet, field.FieldName);
                    if (customField != null && field.Value != null)
                    {
                        newInstance.AddValue(customField.Id, customField.Definition ?? "{}", ValueConverter.Convert(field.Value, customField.Type));
                    }
                }

                await worksheetInstanceRepository.InsertAsync(newInstance);
            }
            else
            {
                foreach (var field in fields)
                {
                    var customField = FindCustomFieldByName(worksheet, field.FieldName);
                    var valueField = worksheetInstance.Values.FirstOrDefault(s => s.CustomFieldId == field.FieldId);
                    if (customField != null && field.Value != null)
                    {
                        valueField?.SetValue(ValueConverter.Convert(field.Value, customField.Type));
                    }
                }
            }
        }

        private static List<ValueFieldContainer> BuildFields(Dictionary<string, string> dictionary)
        {
            var fields = new List<ValueFieldContainer>();

            foreach (var field in dictionary)
            {
                // Field is broken down into {FieldId}.{UiAnchor}.{FieldName} and then value

                var split = field.Key.Split('.', StringSplitOptions.RemoveEmptyEntries);

                fields.Add(new ValueFieldContainer()
                {
                    FieldId = Guid.Parse(split[0]),
                    UiAnchor = split[1],
                    FieldName = split[2],
                    Value = field.Value
                });
            }

            return fields;
        }

        private static CustomField? FindCustomFieldByName(Worksheet? worksheet, string fieldName)
        {
            if (worksheet == null) return null;
            if (worksheet.Sections == null) return null;

            return worksheet.Sections.SelectMany(s => s.Fields).FirstOrDefault(s => s.Name == fieldName);
        }
    }

    public class ValueFieldContainer
    {
        public Guid FieldId { get; set; }
        public string UiAnchor { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
