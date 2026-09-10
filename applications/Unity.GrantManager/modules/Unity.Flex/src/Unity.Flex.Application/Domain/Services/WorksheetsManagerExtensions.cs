using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Domain.Services
{
    public static class WorksheetsManagerExtensions
    {
        public static List<ValueFieldContainer> BuildFields(this Dictionary<string, string> dictionary)
        {
            var fields = new List<ValueFieldContainer>();

            foreach (var field in dictionary)
            {
                // Field is broken down into {FieldName}.{UiAnchor}.{FieldId}.{AdditionalIdentifier} and then value

                var split = field.Key.Split('.', StringSplitOptions.RemoveEmptyEntries);

                fields.Add(new ValueFieldContainer()
                {
                    FieldId = Guid.Parse(split[2]),
                    UiAnchor = split[1],
                    FieldName = split[0],
                    Value = field.Value,
                    AdditionalIdentifier = split.Length > 3 ? split[3] : null,
                });
            }

            return fields;
        }

        public static List<ValueFieldContainer> GroupAndTransformFieldSets(this List<ValueFieldContainer> valueFields, Worksheet? worksheet)
        {
            var groups = valueFields
                .GroupBy(s => s.FieldId)
                .ToList();

            var list = new List<ValueFieldContainer>();

            // Always route through JoinFieldValues, even for a single posted value. A checkbox-group
            // field with exactly one box checked still needs JSON-array encoding (JoinFieldValues
            // dispatches to ConvertCheckboxGroupMultiValues for that type); skipping it here previously
            // left the raw single checkbox value (e.g. "true") stored instead of a JSON array, which
            // the reporting views can't jsonb_array_elements() over. JoinFieldValues already returns the
            // raw single value unchanged for every other field type.
            foreach (var group in groups)
            {
                var fieldId = group.First().FieldId;
                var fieldName = group.First().FieldName;
                var uiAnchor = group.First().UiAnchor;
                var additionalIdentifiers = group.Select(s => s.AdditionalIdentifier).ToList();
                var values = group.Select(s => s.Value).ToList();

                list.Add(new ValueFieldContainer()
                {
                    FieldId = fieldId,
                    FieldName = fieldName,
                    UiAnchor = uiAnchor,
                    Value = values.JoinFieldValues(fieldId, worksheet, additionalIdentifiers)
                });
            }

            return list;
        }

        public static string? JoinFieldValues(this List<string?> fieldValues,
            Guid fieldId,
            Worksheet? worksheet,
            List<string?> additionalIdentifiers)
        {
            var field = worksheet?.Sections.SelectMany(s => s.Fields).FirstOrDefault(s => s.Id == fieldId);

            if (field != null)
            {
                return field.Type switch
                {
                    Flex.Worksheets.CustomFieldType.CheckboxGroup => ConvertCheckboxGroupMultiValues(additionalIdentifiers, fieldValues),
                    _ => fieldValues[0],
                };
            }

            return fieldValues[0];
        }

        internal static string? ConvertCheckboxGroupMultiValues(List<string?> additionalIdentifiers, List<string?> values)
        {
            var keysAndValues = additionalIdentifiers.Zip(values, (n, w) => new { key = n, value = w.IsTruthy() });
            return JsonSerializer.Serialize(new CheckboxGroupValue(keysAndValues).Value);
        }
    }   
}
