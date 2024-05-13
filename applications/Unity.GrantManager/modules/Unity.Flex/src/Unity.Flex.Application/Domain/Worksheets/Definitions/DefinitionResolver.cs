using System;
using System.Text.Json;
using Unity.Flex.Enums;

namespace Unity.Flex.Domain.Worksheets.Definitions
{
    internal static class DefinitionResolver
    {
        internal static string Resolve(CustomFieldType type)
        {
            switch (type)
            {
                case CustomFieldType.Text:
                    return JsonSerializer.Serialize(new Text());
                case CustomFieldType.Numeric:
                    return JsonSerializer.Serialize(new Numeric());
                case CustomFieldType.Currency:
                    return JsonSerializer.Serialize(new Currency());
                case CustomFieldType.Undefined:
                case CustomFieldType.Date:
                case CustomFieldType.DateTime:
                default:
                    throw new NotImplementedException();
            }
        }
    }
}