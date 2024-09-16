using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Unity.Flex.Domain.Utils;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets.Values;
using Volo.Abp.Domain.Services;

namespace Unity.Flex.Domain.Services
{
    public class WorksheetsManager(IWorksheetInstanceRepository worksheetInstanceRepository,
        IWorksheetRepository worksheetRepository,
        IWorksheetLinkRepository worksheetLinkRepository) : DomainService
    {
        public async Task PersistWorksheetData(PersistWorksheetIntanceValuesEto eventData)
        {
            if (eventData.CustomFields is null) { return; }

            string json = JsonSerializer.Serialize(eventData.CustomFields);
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (dictionary == null || dictionary.Count == 0) return;

            var worksheetInstance = await worksheetInstanceRepository.GetByCorrelationAnchorWorksheetAsync(eventData.InstanceCorrelationId,
                eventData.InstanceCorrelationProvider,
                eventData.WorksheetId,
                eventData.UiAnchor,
                true);

            Worksheet? worksheet;
            if (string.IsNullOrEmpty(eventData.FormDataName))
            {
                worksheet = await worksheetRepository.GetByCorrelationAnchorAsync(eventData.SheetCorrelationId, eventData.SheetCorrelationProvider, eventData.UiAnchor, true);
            }
            else
            {
                worksheet = await worksheetRepository.GetByNameAsync(eventData.FormDataName[..eventData.FormDataName.IndexOf("_form")], true);
            }

            var fields = dictionary
                    .BuildFields()
                    .GroupAndTransformFieldSets(worksheet);

            if (worksheetInstance == null)
            {
                if (worksheet == null) return;

                var instance = await CreateNewWorksheetInstanceAsync(worksheetInstanceRepository, eventData, worksheet, fields);
                await UpdateWorksheetInstanceValueAsync(instance);
            }
            else
            {
                UpdateExistingWorksheetInstance(worksheetInstance, worksheet, fields);
                await UpdateWorksheetInstanceValueAsync(worksheetInstance);
            }
        }

        private async Task UpdateWorksheetInstanceValueAsync(WorksheetInstance instance)
        {
            // Update and set the instance value for the worksheet - high level values serialized
            var worksheet = await worksheetRepository.GetAsync(instance.WorksheetId, true);
            var fieldDefinitions = worksheet.Sections.SelectMany(s => s.Fields).ToList();
            var instanceCurrentValue = new WorksheetInstanceValue();
            foreach (var field in instance.Values)
            {
                var fieldDefinition = fieldDefinitions.Find(s => s.Id == field.CustomFieldId);
                if (fieldDefinition != null)
                    instanceCurrentValue.Values.Add(new FieldInstanceValue(fieldDefinition.Key,
                        JsonNode.Parse(field.CurrentValue)?["value"]?.ToString() ?? string.Empty));
            }
            instance.SetValue(JsonSerializer.Serialize(instanceCurrentValue));
        }

        private void UpdateExistingWorksheetInstance(WorksheetInstance worksheetInstance, Worksheet? worksheet, List<ValueFieldContainer> fields)
        {
            foreach (var field in fields)
            {
                try
                {
                    var customField = FindCustomFieldByName(worksheet, field.FieldName);
                    var valueField = worksheetInstance.Values.FirstOrDefault(s => s.CustomFieldId == field.FieldId);
                    if (customField != null && field.Value != null && valueField != null)
                    {
                        valueField.SetValue(ValueConverter.Convert(field.Value, customField.Type));
                    }
                    else
                    {
                        // add the value to worksheet instance
                        if (worksheet != null)
                        {
                            var wsField = worksheet.Sections.SelectMany(s => s.Fields).FirstOrDefault(s => s.Name == field.FieldName);
                            if (wsField != null)
                            {
                                worksheetInstance.AddValue(wsField.Id,
                                    ValueConverter.Convert(field.Value ?? string.Empty, wsField.Type));
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        private async Task<WorksheetInstance> CreateNewWorksheetInstanceAsync(IWorksheetInstanceRepository worksheetInstanceRepository,
            PersistWorksheetIntanceValuesEto eventData,
            Worksheet worksheet,
            List<ValueFieldContainer> fields)
        {
            var newWorksheetInstance = new WorksheetInstance(Guid.NewGuid(),
                   worksheet.Id,
                   eventData.InstanceCorrelationId,
                   eventData.InstanceCorrelationProvider,
                   eventData.SheetCorrelationId,
                   eventData.SheetCorrelationProvider,
                   eventData.UiAnchor);

            foreach (var field in fields)
            {
                try
                {
                    var customField = FindCustomFieldByName(worksheet, field.FieldName);
                    if (customField != null && field.Value != null)
                    {
                        newWorksheetInstance.AddValue(customField.Id,
                            ValueConverter.Convert(field.Value, customField.Type));
                    }
                }
                catch (JsonException ex)
                {
                    Logger.LogException(ex);
                }
            }

            return await worksheetInstanceRepository.InsertAsync(newWorksheetInstance);
        }

        public async Task<List<(Worksheet, WorksheetInstance)>> CreateWorksheetDataByFields(CreateWorksheetInstanceByFieldValuesEto eventData)
        {
            if (eventData.CustomFields.Count == 0) { return []; }
            var worksheetNames = new List<string>();
            var newWorksheetInstances = new List<(Worksheet, WorksheetInstance)>();

            // naming convention custom_worksheetname_fieldname
            foreach (var field in eventData.CustomFields)
            {
                var split = field.Key.Split('_', StringSplitOptions.RemoveEmptyEntries);

                if (!worksheetNames.Contains(split[1]))
                {
                    worksheetNames.Add(split[1]);
                }
            }

            foreach (var worksheetName in worksheetNames)
            {
                var worksheet = await worksheetRepository.GetByCorrelationByNameAsync(eventData.SheetCorrelationId, eventData.SheetCorrelationProvider, worksheetName, true);

                if (worksheet != null)
                {
                    var worksheetLink = await worksheetLinkRepository.GetExistingLinkAsync(worksheet.Id, eventData.SheetCorrelationId, eventData.SheetCorrelationProvider);

                    if (worksheetLink != null)
                    {
                        var newInstance = new WorksheetInstance(Guid.NewGuid(),
                         worksheet.Id,
                         eventData.InstanceCorrelationId,
                         eventData.InstanceCorrelationProvider,
                         eventData.SheetCorrelationId,
                         eventData.SheetCorrelationProvider,
                         worksheetLink.UiAnchor);

                        var allFields = worksheet.Sections.SelectMany(s => s.Fields);

                        foreach (var field in allFields)
                        {
                            var match = eventData.CustomFields.Find(s => s.Key == field.Name);
                            newInstance.AddValue(field.Id,
                                ValueConverter.Convert(match.Value?.ToString() ?? string.Empty, field.Type));
                        }

                        var newWorksheetInstance = await worksheetInstanceRepository.InsertAsync(newInstance);
                        await UpdateWorksheetInstanceValueAsync(newWorksheetInstance);
                        newWorksheetInstances.Add(new(worksheet, newWorksheetInstance));
                    }
                }
            }

            return newWorksheetInstances;
        }

        public async Task<Worksheet> CloneWorksheetAsync(Guid id)
        {
            var worksheet = await worksheetRepository.GetAsync(id, true);
            var versionSplit = SheetParserFunctions.SplitSheetNameAndVersion(worksheet.Name);
            var worksheetVersions = await worksheetRepository.GetByNameStartsWithAsync($"{versionSplit[0]}-v", false);
            var highestVersion = worksheetVersions.Max(s => s.Version);
            var clonedWorksheet = new Worksheet(Guid.NewGuid(), $"{versionSplit[0]}-v{highestVersion + 1}", worksheet.Title);
            clonedWorksheet.SetVersion(highestVersion + 1);
            foreach (var section in worksheet.Sections.OrderBy(s => s.Order))
            {
                var clonedSection = new WorksheetSection(Guid.NewGuid(), section.Name);
                foreach (var field in section.Fields.OrderBy(s => s.Order))
                {
                    var clonedField = new CustomField(Guid.NewGuid(), field.Key, worksheet.Name, field.Label, field.Type, field.Definition);
                    clonedSection.CloneField(clonedField);
                }
                clonedWorksheet.CloneSection(clonedSection);
            }

            var result = await worksheetRepository.InsertAsync(clonedWorksheet);
            return result;
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
        public string? AdditionalIdentifier { get; set; } = string.Empty;
    }
}
