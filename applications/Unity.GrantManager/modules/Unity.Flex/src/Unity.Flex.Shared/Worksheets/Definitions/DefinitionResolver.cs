using System;
using System.Text.Json;

namespace Unity.Flex.Worksheets
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
                _ => throw new NotImplementedException(),
            };
        }
    }
}