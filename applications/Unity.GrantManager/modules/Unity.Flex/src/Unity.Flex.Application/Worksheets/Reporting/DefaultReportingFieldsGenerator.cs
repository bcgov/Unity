using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Worksheets.Reporting
{
    public class DefaultReportingFieldsGenerator(CustomField customField, char separator, uint maxColumnLength)
        : ReportingFieldsGeneratorBase(customField, separator, maxColumnLength), IReportingFieldsGenerator
    {
        public (string columns, string keys) Generate()
        {
            return (customField.Name, customField.Key);
        }
    }
}
