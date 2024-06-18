using System;
using System.Text.Json;

namespace Unity.Flex.Worksheets.Definitions
{
    public static class DefinitionResolver
    {
        public static string Resolve(CustomFieldType type, object? definition)
        {
            return type switch
            {
                CustomFieldType.Undefined => definition == null ? "{}" : ((JsonElement)definition).ToString(),
                CustomFieldType.Numeric => definition == null ? JsonSerializer.Serialize(new NumericDefinition()) : ((JsonElement)definition).ToString(),
                CustomFieldType.Text => definition == null ? JsonSerializer.Serialize(new TextDefinition()) : ((JsonElement)definition).ToString(),                                
                CustomFieldType.Date => definition == null ? JsonSerializer.Serialize(new DateDefinition()) : ((JsonElement)definition).ToString(),
                CustomFieldType.DateTime => definition == null ? JsonSerializer.Serialize(new DateTimeDefinition()) : ((JsonElement)definition).ToString(),
                CustomFieldType.Currency => definition == null ? JsonSerializer.Serialize(new CurrencyDefinition()) : ((JsonElement)definition).ToString(),
                CustomFieldType.YesNo => definition == null ? JsonSerializer.Serialize(new YesNoDefinition()) : ((JsonElement)definition).ToString(),
                CustomFieldType.Email => definition == null ? JsonSerializer.Serialize(new EmailDefinition()) : ((JsonElement)definition).ToString(),
                CustomFieldType.Phone => definition == null ? JsonSerializer.Serialize(new PhoneDefinition()) : ((JsonElement)definition).ToString(),                
                CustomFieldType.Radio => definition == null ? JsonSerializer.Serialize(new RadioDefinition()) : ((JsonElement)definition).ToString(),
                CustomFieldType.Checkbox => definition == null ? JsonSerializer.Serialize(new CheckboxDefinition()) : ((JsonElement)definition).ToString(),
                CustomFieldType.CheckboxGroup => definition == null ? JsonSerializer.Serialize(new CheckboxGroupDefinition()) : ((JsonElement)definition).ToString(),
                CustomFieldType.SelectList => definition == null ? JsonSerializer.Serialize(new SelectListDefinition()) : ((JsonElement)definition).ToString(),
                _ => throw new NotImplementedException(),
            };
        }
    }
}