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
        /// <summary>
        /// Maps Forms.io field types to PostgreSQL data types.
        /// </summary>
        /// <param name="formsIoType">The Forms.io field type.</param>
        /// <returns>The corresponding PostgreSQL data type name.</returns>
        public static string MapToPostgreSqlType(string formsIoType)
        {
            return formsIoType?.ToLowerInvariant() switch
            {
                "textfield" or "textarea" or "email" or "password" or "url" or "select" => "TEXT",
                "phonenumber" => "TEXT", // Store phone numbers as text to preserve formatting
                "number" or "currency" => "NUMERIC",
                "datetime" or "day" => "TIMESTAMP",
                "option" or "checkbox" or "radio" => "BOOLEAN",
                "file" => "TEXT", // File paths/URLs as text
                "signature" => "TEXT", // Signature data as text
                "hidden" => "TEXT", // Hidden fields as text
                "tags" => "TEXT", // Tags as comma-separated text
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
                "textfield" => $"'Sample {fieldName}'",
                "textarea" => $"'This is sample text for {fieldName} field.'",
                "email" => "'sample@example.com'",
                "password" => "'********'",
                "url" => "'https://example.com'",
                "phonenumber" => "'(555) 123-4567'",
                "number" => "123.45",
                "currency" => "1234.56",
                "datetime" => "'2024-01-01 12:00:00'",
                "day" => "'2024-01-01'",
                "option" or "checkbox" or "radio" => "true",
                "select" => $"'Option 1 for {fieldName}'",
                "file" => "'/uploads/sample-file.pdf'",
                "signature" => "'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=='",
                "hidden" => $"'hidden_{fieldName}_value'",
                "tags" => "'tag1,tag2,tag3'",
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
                "panel" or "fieldset" or "well" or "columns" or "tabs" or "container" => true,
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
                "textfield", "textarea", "email", "password", "url", "select",
                "phonenumber", "number", "currency", "datetime", "day",
                "option", "checkbox", "radio", "file", "signature", "hidden", "tags"
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
                "textfield" => "Single-line text input",
                "textarea" => "Multi-line text input",
                "email" => "Email address input",
                "password" => "Password input (hidden text)",
                "url" => "URL/web address input",
                "phonenumber" => "Phone number input",
                "number" => "Numeric input",
                "currency" => "Currency/monetary value input",
                "datetime" => "Date and time picker",
                "day" => "Date picker (day only)",
                "option" => "Single option from a group",
                "checkbox" => "True/false checkbox",
                "radio" => "Single selection from radio buttons",
                "select" => "Dropdown selection list",
                "file" => "File upload field",
                "signature" => "Digital signature capture",
                "hidden" => "Hidden field (not visible to user)",
                "tags" => "Tag input field",
                "panel" => "Container panel (parent field)",
                "fieldset" => "Field grouping container (parent field)",
                "well" => "Well container (parent field)",
                "columns" => "Column layout container (parent field)",
                "tabs" => "Tabbed container (parent field)",
                "container" => "Generic container (parent field)",
                _ => "Unknown field type"
            };
        }
    }
}