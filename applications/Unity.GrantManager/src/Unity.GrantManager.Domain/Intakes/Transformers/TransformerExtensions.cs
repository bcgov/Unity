using Newtonsoft.Json.Linq;
using System;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.GrantManager.Intakes
{
    public static class TransformerExtensions
    {
        public static CustomValueBase ApplyTransformer(this JToken token, CustomFieldType type)
        {
            // Transform the raw data object from CHEFS to UNITY
            switch (type)
            {
                case CustomFieldType.CheckboxGroup:
                    return new CheckboxGroupTransformer().Transform(token);
                case CustomFieldType.Currency:
                    return new CurrencyValue(token);
                 
                default:
                    throw new NotImplementedException();
            }            
        }
    }
}
