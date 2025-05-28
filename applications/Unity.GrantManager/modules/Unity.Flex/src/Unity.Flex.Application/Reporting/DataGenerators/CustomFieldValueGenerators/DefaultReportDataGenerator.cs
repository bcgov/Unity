using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.DataGenerators.CustomFieldValueGenerators
{
    public class DefaultReportDataGenerator(CustomField customField, CustomFieldValue value)
        : ReportingDataGenerator(customField, value), IReportingDataGenerator
    {
        /// <summary>
        /// Default key values pairing for the reporting data generation
        /// </summary>
        /// <returns>Dictionary with keys and matched values for reporting data</returns>
        public (Dictionary<string, List<string>> keyValuePairs, bool compressArray) Generate()
        {
            JObject dataValue = JObject.Parse(value.CurrentValue);

            var keyValuePairings = new Dictionary<string, List<string>>
            {
                { customField.Key, new List<string>() { dataValue["value"]?.ToString() ?? string.Empty } }
            };

            return (keyValuePairings, true);
        }
    }
}
