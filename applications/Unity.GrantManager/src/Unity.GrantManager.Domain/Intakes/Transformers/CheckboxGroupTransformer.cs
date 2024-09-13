using Newtonsoft.Json.Linq;
using Unity.Flex.Worksheets.Values;
using System.Collections.Generic;

namespace Unity.GrantManager.Intakes
{
    public class CheckboxGroupTransformer : IValueTransformer<JToken, CheckboxGroupValue>
    {
        public CheckboxGroupValue Transform(JToken value)
        {
            // Post from CHEFS checkboxgroup value                                                       
            var checkboxValues = new List<CheckboxGroupValueOption>();            
            JObject obj = JObject.FromObject(value);
            foreach (var prop in obj.Properties())
            {
                checkboxValues.Add(new CheckboxGroupValueOption()
                {
                    Key = prop.Name,
                    Value = bool.Parse(prop.Value.ToString())
                });
            }

            return new CheckboxGroupValue(checkboxValues);
        }
    }
}
