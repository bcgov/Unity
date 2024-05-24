using System;
using System.Text.Json;

namespace Unity.Flex.Worksheets.Definitions
{
    public static class DefinitionResolver
    {
        public static string Resolve(CustomFieldType type)
        {
            return type switch
            {
                CustomFieldType.Text => JsonSerializer.Serialize(new TextDefinition()),
                CustomFieldType.Numeric => JsonSerializer.Serialize(new NumericDefinition()),
                CustomFieldType.Currency => JsonSerializer.Serialize(new CurrencyDefinition()),
                CustomFieldType.Date => JsonSerializer.Serialize(new DateDefinition()),
                CustomFieldType.DateTime => JsonSerializer.Serialize(new DateTimeDefinition()),
                CustomFieldType.YesNo => JsonSerializer.Serialize(new YesNoDefinition()),
                _ => throw new NotImplementedException(),
            };
        }
    }
}