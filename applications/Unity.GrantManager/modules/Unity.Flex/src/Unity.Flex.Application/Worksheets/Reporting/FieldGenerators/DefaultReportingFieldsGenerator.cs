using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Worksheets.Reporting.FieldGenerators
{
    public class DefaultReportingFieldsGenerator(CustomField customField, char separator, uint maxColumnLength)
        : ReportingFieldsGeneratorBase(customField, separator, maxColumnLength), IReportingFieldsGenerator
    {
        public (string columns, string keys) Generate()
        {
            // We intentionally use the key twice here
            return (customField.Key, customField.Key);
        }
    }
}
