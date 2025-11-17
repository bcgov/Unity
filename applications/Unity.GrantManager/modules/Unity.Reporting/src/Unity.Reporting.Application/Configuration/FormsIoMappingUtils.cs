using System;
using System.Collections.Generic;
using System.Text.Json;
using Unity.Reporting.Domain.Configuration;

namespace Unity.Reporting.Configuration
{
    /// <summary>
    /// Utility class for working with Forms.io field mappings and data types.
    /// </summary>
    public static class FormsIoMappingUtils
    {
        private const string TEXTFIELD = "textfield";
        private const string TEXTAREA = "textarea";
        private const string EMAIL = "email";
        private const string PASSWORD = "password";
        private const string URL = "url";
        private const string SELECT = "select";
        private const string PHONENUMBER = "phonenumber";
        private const string NUMBER = "number";
        private const string CURRENCY = "currency";
        private const string DATETIME = "datetime";
        private const string DAY = "day";
        private const string OPTION = "option";
        private const string CHECKBOX = "checkbox";
        private const string RADIO = "radio";
        private const string FILE = "file";
        private const string SIGNATURE = "signature";
        private const string HIDDEN = "hidden";
        private const string TAGS = "tags";
        private const string PANEL = "panel";
        private const string FIELDSET = "fieldset";
        private const string WELL = "well";
        private const string COLUMNS = "columns";
        private const string TABS = "tabs";
        private const string CONTAINER = "container";

        /// <summary>
        /// Maps Forms.io field types to PostgreSQL data types.
        /// </summary>
        /// <param name="formsIoType">The Forms.io field type.</param>
        /// <returns>The corresponding PostgreSQL data type name.</returns>
        public static string MapToPostgreSqlType(string formsIoType)
        {
            return formsIoType?.ToLowerInvariant() switch
            {
                TEXTFIELD or TEXTAREA or EMAIL or PASSWORD or URL or SELECT => "TEXT",
                PHONENUMBER => "TEXT", // Store phone numbers as text to preserve formatting
                NUMBER or CURRENCY => "NUMERIC",
                DATETIME or DAY => "TIMESTAMP",
                OPTION or CHECKBOX or RADIO => "BOOLEAN",
                FILE => "TEXT", // File paths/URLs as text
                SIGNATURE => "TEXT", // Signature data as text
                HIDDEN => "TEXT", // Hidden fields as text
                TAGS => "TEXT", // Tags as comma-separated text
                _ => "TEXT" // Default to TEXT for unknown types
            };
        }

        /// <summary>
        /// Generates appropriate mock data for a given Forms.io field type.
        /// </summary>
        /// <param name="formsIoType">The Forms.io field type.</param>
        /// <param name="fieldName">The field name to use in generating contextual mock data.</param>
        /// <returns>A mock value appropriate for the field type.</returns>
        public static string GenerateMockData(string formsIoType, string fieldName)
        {
            return formsIoType?.ToLowerInvariant() switch
            {
                TEXTFIELD => $"'Sample {fieldName}'",
                TEXTAREA => $"'This is sample text for {fieldName} field.'",
                EMAIL => "'sample@example.com'",
                PASSWORD => "'********'",
                URL => "'https://example.com'",
                PHONENUMBER => "'(555) 123-4567'",
                NUMBER => "123.45",
                CURRENCY => "1234.56",
                DATETIME => "'2024-01-01 12:00:00'",
                DAY => "'2024-01-01'",
                OPTION or CHECKBOX or RADIO => "true",
                SELECT => $"'Option 1 for {fieldName}'",
                FILE => "'/uploads/sample-file.pdf'",
                SIGNATURE => "'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=='",
                HIDDEN => $"'hidden_{fieldName}_value'",
                TAGS => "'tag1,tag2,tag3'",
                _ => $"'Sample {fieldName}'" // Default mock data
            };
        }

        /// <summary>
        /// Determines if a Forms.io field type represents a parent/container field.
        /// </summary>
        /// <param name="formsIoType">The Forms.io field type.</param>
        /// <returns>True if the field is a container/parent field, false otherwise.</returns>
        public static bool IsParentField(string formsIoType)
        {
            return formsIoType?.ToLowerInvariant() switch
            {
                PANEL or FIELDSET or WELL or COLUMNS or TABS or CONTAINER => true,
                _ => false
            };
        }

        /// <summary>
        /// Extracts mapping rows from a ReportColumnsMap entity, deserializing the JSON mapping.
        /// </summary>
        /// <param name="reportColumnsMap">The ReportColumnsMap entity containing the JSON mapping.</param>
        /// <returns>An array of MapRow objects, or an empty array if deserialization fails.</returns>
        public static MapRow[] ExtractMappingRows(ReportColumnsMap reportColumnsMap)
        {
            if (string.IsNullOrWhiteSpace(reportColumnsMap.Mapping))
            {
                return [];
            }

            try
            {
                var mapping = JsonSerializer.Deserialize<Mapping>(reportColumnsMap.Mapping);
                return mapping?.Rows ?? [];
            }
            catch (JsonException)
            {
                // If deserialization fails, return empty array
                return [];
            }
        }

        /// <summary>
        /// Validates that a Forms.io field type is supported for database view generation.
        /// </summary>
        /// <param name="formsIoType">The Forms.io field type to validate.</param>
        /// <returns>True if the field type is supported, false otherwise.</returns>
        public static bool IsSupportedFieldType(string formsIoType)
        {
            var supportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                TEXTFIELD, TEXTAREA, EMAIL, PASSWORD, URL, SELECT,
                PHONENUMBER, NUMBER, CURRENCY, DATETIME, DAY,
                OPTION, CHECKBOX, RADIO, FILE, SIGNATURE, HIDDEN, TAGS
            };

            return supportedTypes.Contains(formsIoType ?? string.Empty);
        }

        /// <summary>
        /// Gets a description of the Forms.io field type for documentation purposes.
        /// </summary>
        /// <param name="formsIoType">The Forms.io field type.</param>
        /// <returns>A human-readable description of the field type.</returns>
        public static string GetFieldTypeDescription(string formsIoType)
        {
            return formsIoType?.ToLowerInvariant() switch
            {
                TEXTFIELD => "Single-line text input",
                TEXTAREA => "Multi-line text input",
                EMAIL => "Email address input",
                PASSWORD => "Password input (hidden text)",
                URL => "URL/web address input",
                PHONENUMBER => "Phone number input",
                NUMBER => "Numeric input",
                CURRENCY => "Currency/monetary value input",
                DATETIME => "Date and time picker",
                DAY => "Date picker (day only)",
                OPTION => "Single option from a group",
                CHECKBOX => "True/false checkbox",
                RADIO => "Single selection from radio buttons",
                SELECT => "Dropdown selection list",
                FILE => "File upload field",
                SIGNATURE => "Digital signature capture",
                HIDDEN => "Hidden field (not visible to user)",
                TAGS => "Tag input field",
                PANEL => "Container panel (parent field)",
                FIELDSET => "Field grouping container (parent field)",
                WELL => "Well container (parent field)",
                COLUMNS => "Column layout container (parent field)",
                TABS => "Tabbed container (parent field)",
                CONTAINER => "Generic container (parent field)",
                _ => "Unknown field type"
            };
        }
    }
}