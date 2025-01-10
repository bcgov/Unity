using System.Collections.Generic;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Reporting.DataGenerators.CustomFieldValueGenerators
{
    public class DefaultReportDataGenerator(CustomField customField, FieldInstanceValue value)
        : ReportingDataGeneratorBase(customField, value), IReportingDataGenerator
    {
        public Dictionary<string, List<string>> Generate()
        {
            return new Dictionary<string, List<string>>
            {
                { value.Key, new List<string>() { value.Value } }
            };
        }
    }
}
