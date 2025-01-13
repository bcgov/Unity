using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class DefaultReportingFieldsGenerator(CustomField customField)
        : CustomFieldsReportingGenerator(customField), IReportingFieldsGenerator
    {
        public (string keys, string columns) Generate()
        {
            // We intentionally use the key twice here
            return (customField.Key, customField.Key);
        }
    }
}
