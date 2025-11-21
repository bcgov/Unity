using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Reporting.Configuration
{
    /// <summary>
    /// Utility class for parsing worksheet field schemas and generating metadata components,
    /// especially for complex types like DataGrid that can contain multiple sub-components.
    /// </summary>
    public static partial class WorksheetFieldSchemaParser
    {
        private const string UnknownSectionName = "unknown_section";

        /// <summary>
        /// Parses a custom field and returns component metadata items.
        /// For simple fields, returns a single item. For complex fields like DataGrid, 
        /// returns multiple items based on the field definition.
        /// </summary>
        /// <param name="field">The custom field to parse</param>
        /// <param name="worksheet">The worksheet containing the field (for name context)</param>
        /// <returns>List of component metadata items</returns>
        public static List<WorksheetComponentMetaDataItemDto> ParseField(CustomField field, Worksheet worksheet)
        {
            if (field == null)
                return [];

            var components = new List<WorksheetComponentMetaDataItemDto>();

            switch (field.Type)
            {
                case CustomFieldType.DataGrid:
                    components.AddRange(ParseDataGridField(field, worksheet));
                    break;
                
                case CustomFieldType.CheckboxGroup:
                    components.AddRange(ParseCheckboxGroupField(field, worksheet));
                    break;
                
                case CustomFieldType.Radio:
                    components.AddRange(ParseRadioField(field, worksheet));
                    break;
                
                default:
                    // For simple field types, return a single component
                    components.Add(CreateSimpleComponent(field, worksheet));
                    break;
            }

            return components;
        }

        /// <summary>
        /// Parses all fields in a worksheet and returns flattened component metadata.
        /// </summary>
        /// <param name="worksheet">The worksheet to parse</param>
        /// <returns>List of all component metadata items</returns>
        public static List<WorksheetComponentMetaDataItemDto> ParseWorksheet(Worksheet worksheet)
        {
            if (worksheet?.Sections == null)
                return [];

            return [..worksheet.Sections
                .Where(section => section.Fields != null)
                .SelectMany(section => section.Fields)
                .SelectMany(field => ParseField(field, worksheet))];                
        }

        /// <summary>
        /// Parses a DataGrid field and returns metadata for each column defined in the DataGrid definition.
        /// If dynamic is true, creates a placeholder column for dynamically determined columns.
        /// </summary>
        /// <param name="field">The DataGrid field to parse</param>
        /// <param name="worksheet">The worksheet containing the field</param>
        /// <returns>List of component metadata items for each DataGrid column</returns>
        private static List<WorksheetComponentMetaDataItemDto> ParseDataGridField(CustomField field, Worksheet worksheet)
        {
            var components = new List<WorksheetComponentMetaDataItemDto>();

            try
            {
                // Parse the DataGrid definition from the field's Definition JSON
                var dataGridDefinition = JsonSerializer.Deserialize<DataGridDefinition>(field.Definition);
                
                if (dataGridDefinition == null)
                {
                    // If deserialization fails, return the DataGrid itself as a component
                    return [CreateSimpleComponent(field, worksheet)];
                }

                // Get section name for path construction
                var section = worksheet.Sections.FirstOrDefault(s => s.Id == field.SectionId);
                var sectionName = SanitizeName(section?.Name ?? UnknownSectionName);
                var worksheetName = SanitizeName(worksheet.Name);
                var dataGridName = SanitizeName(field.Key);

                // If dynamic is true, create a placeholder for dynamically determined columns
                if (dataGridDefinition.Dynamic)
                {
                    var dynamicComponent = new WorksheetComponentMetaDataItemDto
                    {
                        Id = $"{field.Id}_dynamic",
                        Key = "dynamic_columns",
                        Label = "Dynamic Columns",
                        Type = "Dynamic",
                        Path = $"{worksheetName}->{sectionName}->{dataGridName}->dynamic_columns",
                        TypePath = $"worksheet->section->datagrid->Dynamic",
                        DataPath = $"({worksheetName}){dataGridName}->dynamic_columns"
                    };
                    
                    components.Add(dynamicComponent);
                }

                // Process additional defined columns (if any)
                if (dataGridDefinition.Columns != null && dataGridDefinition.Columns.Count > 0)
                {
                    // Create a component for each column in the DataGrid
                    foreach (var column in dataGridDefinition.Columns)
                    {
                        var columnName = SanitizeName(column.Name);
                        
                        var component = new WorksheetComponentMetaDataItemDto
                        {
                            Id = $"{field.Id}_{columnName}",
                            Key = column.Name,
                            Label = column.Name,
                            Type = MapDataGridColumnType(column.Type),
                            Path = $"{worksheetName}->{sectionName}->{dataGridName}->{column.Name}",
                            TypePath = $"worksheet->section->datagrid->{MapDataGridColumnType(column.Type)}",
                            DataPath = $"({worksheetName}){dataGridName}->{column.Name}"
                        };
                        
                        components.Add(component);
                    }
                }
                else if (!dataGridDefinition.Dynamic)
                {
                    // If no columns defined and not dynamic, return the DataGrid itself as a component
                    return [CreateSimpleComponent(field, worksheet)];
                }

                // Return at least one component (either dynamic placeholder or defined columns or both)
                return components.Count > 0 ? components : [CreateSimpleComponent(field, worksheet)];
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return the field as a simple component
                return [CreateSimpleComponent(field, worksheet)];
            }
        }

        /// <summary>
        /// Parses a CheckboxGroup field and returns metadata for each checkbox option defined in the CheckboxGroup definition.
        /// </summary>
        /// <param name="field">The CheckboxGroup field to parse</param>
        /// <param name="worksheet">The worksheet containing the field</param>
        /// <returns>List of component metadata items for each checkbox option</returns>
        private static List<WorksheetComponentMetaDataItemDto> ParseCheckboxGroupField(CustomField field, Worksheet worksheet)
        {
            var components = new List<WorksheetComponentMetaDataItemDto>();

            try
            {
                // Parse the CheckboxGroup definition from the field's Definition JSON
                var checkboxGroupDefinition = JsonSerializer.Deserialize<CheckboxGroupDefinition>(field.Definition);
                
                if (checkboxGroupDefinition?.Options == null || checkboxGroupDefinition.Options.Count <= 0)
                {
                    // If no options defined, return the CheckboxGroup itself as a component
                    return [CreateSimpleComponent(field, worksheet)];
                }

                // Get section name for path construction
                var section = worksheet.Sections.FirstOrDefault(s => s.Id == field.SectionId);
                var sectionName = SanitizeName(section?.Name ?? UnknownSectionName);
                var worksheetName = SanitizeName(worksheet.Name);
                var checkboxGroupName = SanitizeName(field.Key);

                // Create a component for each option in the CheckboxGroup
                foreach (var option in checkboxGroupDefinition.Options)
                {
                    var optionKey = SanitizeName(option.Key);
                    
                    var component = new WorksheetComponentMetaDataItemDto
                    {
                        Id = $"{field.Id}_{optionKey}",
                        Key = option.Key,
                        Label = option.Label,
                        Type = "Checkbox", // Each option is essentially a checkbox
                        Path = $"{worksheetName}->{sectionName}->{checkboxGroupName}->{option.Key}",
                        TypePath = $"worksheet->section->checkboxgroup->Checkbox",
                        DataPath = $"({worksheetName}){checkboxGroupName}->{option.Key}"
                    };
                    
                    components.Add(component);
                }
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return the field as a simple component
                return [CreateSimpleComponent(field, worksheet)];
            }

            return components;
        }

        /// <summary>
        /// Parses a Radio field and returns metadata as a single component since radio fields
        /// represent a single choice from multiple options.
        /// </summary>
        /// <param name="field">The Radio field to parse</param>
        /// <param name="worksheet">The worksheet containing the field</param>
        /// <returns>Single component metadata item for the radio field</returns>
        private static List<WorksheetComponentMetaDataItemDto> ParseRadioField(CustomField field, Worksheet worksheet)
        {
            try
            {
                // Parse the Radio definition from the field's Definition JSON to validate it
                var radioDefinition = JsonSerializer.Deserialize<RadioDefinition>(field.Definition);
                
                if (radioDefinition?.Options == null || radioDefinition.Options.Count <= 0)
                {
                    // If no options defined, return the Radio field itself as a component
                    return [CreateSimpleComponent(field, worksheet)];
                }

                // Radio fields should be treated as a single component since only one option can be selected
                // This creates one reporting column for the entire radio group
                return [CreateSimpleComponent(field, worksheet)];
            }
            catch (JsonException)
            {
                // If JSON parsing fails, return the field as a simple component
                return [CreateSimpleComponent(field, worksheet)];
            }
        }

        /// <summary>
        /// Creates a simple component metadata item for non-complex field types.
        /// </summary>
        /// <param name="field">The field to create a component for</param>
        /// <param name="worksheet">The worksheet containing the field</param>
        /// <returns>Component metadata item</returns>
        private static WorksheetComponentMetaDataItemDto CreateSimpleComponent(CustomField field, Worksheet worksheet)
        {
            var section = worksheet.Sections.FirstOrDefault(s => s.Id == field.SectionId);
            var sectionName = SanitizeName(section?.Name ?? UnknownSectionName);
            var worksheetName = SanitizeName(worksheet.Name);
            var fieldName = SanitizeName(field.Key);

            return new WorksheetComponentMetaDataItemDto
            {
                Id = field.Id.ToString(),
                Key = field.Key,
                Label = field.Label,
                Type = field.Type.ToString(),
                Path = $"{worksheetName}->{sectionName}->{fieldName}",
                TypePath = $"worksheet->section->{field.Type.ToString().ToLowerInvariant()}",
                DataPath = $"({worksheetName}){fieldName}"
            };
        }

        /// <summary>
        /// Maps DataGrid column types to standard field types.
        /// </summary>
        /// <param name="columnType">The DataGrid column type</param>
        /// <returns>Mapped field type string</returns>
        private static string MapDataGridColumnType(string columnType)
        {
            return columnType?.ToLowerInvariant() switch
            {
                "text" => "Text",
                "numeric" => "Numeric", 
                "currency" => "Currency",
                "checkbox" => "Checkbox",
                "date" => "Date",
                "datetime" => "DateTime",
                _ => "Text" // Default to Text for unknown types
            };
        }

        /// <summary>
        /// Sanitizes names for use in paths by removing special characters and spaces.
        /// </summary>
        /// <param name="name">The name to sanitize</param>
        /// <returns>Sanitized name</returns>
        private static string SanitizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "unknown";

            // Keep alphanumeric characters, underscores, and hyphens
            // Replace spaces with underscores
            return SantizedNameExpression().Replace(name.Trim().Replace(" ", "_"), "");
        }

        [GeneratedRegex(@"[^a-zA-Z0-9_\-]")]
        private static partial Regex SantizedNameExpression();
    }
}