using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class DefaultReportingFieldsGenerator(CustomField customField, char separator, uint maxColumnLength)
        : CustomFieldsReportingGenerator(customField, separator, maxColumnLength), IReportingFieldsGenerator
    {
        public (string columns, string keys) Generate()
        {
            // We intentionally use the key twice here
            return (customField.Key, customField.Key);
        }
    }
}
