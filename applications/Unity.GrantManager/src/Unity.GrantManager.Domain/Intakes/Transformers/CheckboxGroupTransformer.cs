using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets.Transformers;
using Unity.Flex.Worksheets.Values;

namespace Unity.GrantManager.Intakes
{
    public class CheckboxGroupTransformer : IValueTransformer<JToken, CheckboxGroupValue>
    {
        public CheckboxGroupValue Transform(JToken value)
        {
            // raw post from CHEFS object                                
            var fieldDefinition = new CheckboxGroupDefinition();
            var checkBoxGroupValueOptions = new List<CheckboxGroupValueOption>();
            foreach (CheckboxOption check in fieldDefinition.Options)
            {
                JToken? jToken = ((JObject)value).SelectToken(check.Key);
                var fieldOption = fieldDefinition.Options.Find(s => s.Key == check.Key);
                if (fieldOption != null)
                {
                    checkBoxGroupValueOptions.Add(new CheckboxGroupValueOption()
                    {
                        Key = fieldOption.Key,
                        Value = bool.Parse(jToken?.SelectToken(check.Key)?.ToString() ?? "false")
                    });
                }
            }
            return new CheckboxGroupValue(checkBoxGroupValueOptions.ToArray());
        }
    }
}
