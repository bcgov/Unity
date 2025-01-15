using System.Collections.Generic;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Reporting.DataGenerators.CustomFieldValueGenerators
{
    public class DefaultReportDataGenerator(CustomField customField, FieldInstanceValue value)
        : ReportingDataGenerator(customField, value), IReportingDataGenerator
    {
        /// <summary>
        /// Default key values pairing for the reporting data generation
        /// </summary>
        /// <returns>Dictionary with keys and matched values for reporting data</returns>
        public Dictionary<string, List<string>> Generate()
        {
            return new Dictionary<string, List<string>>
            {
                { value.Key, new List<string>() { value.Value } }
            };
        }
    }
}
