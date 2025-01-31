using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Flex.Worksheets;

namespace Unity.Flex
{
    internal static class ChefsToUnityTypes
    {
        internal static string Convert(string input, string defaultValue)
        {
            var found = TypePairs.TryGetValue(input, out string? value);
            if (found)
                return value ?? CustomFieldType.Text.ToString();
            else
                return defaultValue ?? CustomFieldType.Text.ToString();
        }

        // Define the pairs collection
        internal static readonly Dictionary<string, string> TypePairs =
            new()
            {
                { "textarea", CustomFieldType.TextArea.ToString() },
                { "orgbook", CustomFieldType.Text.ToString() },
                { "textfield", CustomFieldType.Text.ToString() },
                { "currency", CustomFieldType.Currency.ToString() },
                { "datetime", CustomFieldType.DateTime.ToString() },
                { "checkbox", CustomFieldType.Checkbox.ToString() },
                { "select", CustomFieldType.SelectList.ToString() },
                { "selectboxes", CustomFieldType.CheckboxGroup.ToString() },
                { "radio", CustomFieldType.Radio.ToString() },
                { "phoneNumber", CustomFieldType.Phone.ToString() },
                { "email", CustomFieldType.Email.ToString() },
                { "number", CustomFieldType.Numeric.ToString() },
                { "time", CustomFieldType.Text.ToString() },
                { "day", CustomFieldType.Text.ToString() },
                { "hidden", CustomFieldType.Text.ToString() },
                { "simpletextfield", CustomFieldType.Text.ToString() },
                { "simpletextfieldadvanced", CustomFieldType.Text.ToString() },
                { "simpletime", CustomFieldType.Text.ToString() },
                { "simpletimeadvanced", CustomFieldType.Text.ToString() },
                { "simplenumber", CustomFieldType.Numeric.ToString() },
                { "simplenumberadvanced", CustomFieldType.Numeric.ToString() },
                { "simplephonenumber", CustomFieldType.Phone.ToString() },
                { "simplephonenumberadvanced", CustomFieldType.Phone.ToString() },
                { "simpleselect", CustomFieldType.SelectList.ToString() },
                { "simpleselectadvanced", CustomFieldType.SelectList.ToString() },
                { "simpleday", CustomFieldType.Text.ToString() },
                { "simpledayadvanced", CustomFieldType.Text.ToString() },
                { "simpleemail", CustomFieldType.Email.ToString() },
                { "simpleemailadvanced", CustomFieldType.Email.ToString() },
                { "simpledatetime", CustomFieldType.DateTime.ToString() },
                { "simpledatetimeadvanced", CustomFieldType.DateTime.ToString() },
                { "simpleurladvanced", CustomFieldType.Text.ToString() },
                { "simplecheckbox", CustomFieldType.Checkbox.ToString() },
                { "simpleradios", CustomFieldType.Radio.ToString() },
                { "simpleradioadvanced", CustomFieldType.Radio.ToString() },
                { "simplecheckboxes", CustomFieldType.CheckboxGroup.ToString() },
                { "simplecheckboxadvanced", CustomFieldType.CheckboxGroup.ToString() },
                { "simplecurrencyadvanced", CustomFieldType.Currency.ToString() },
                { "simpletextarea", CustomFieldType.TextArea.ToString() },
                { "simpletextareaadvanced", CustomFieldType.TextArea.ToString() },
                { "bcaddress", CustomFieldType.BCAddress.ToString() },
                { "datagrid", CustomFieldType.DataGrid.ToString() }
            };

        internal static bool RequiresDateTimeConversion(this string type)
        {
            return type == CustomFieldType.DateTime.ToString();
        }

        internal static string ApplyDateTimeConversion(this JToken token)
        {
            if (DateTime.TryParse(token.ToString(), new CultureInfo("en-CA"), out _))
            {
                // If the value is a DateTime, keep the raw format
                return token.ToString(Newtonsoft.Json.Formatting.None).Trim('"');
            }

            return token.ToString();
        }
    }
}
