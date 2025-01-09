using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class CustomFieldsReportingGenerator
    {
        protected readonly CustomField customField;
        protected readonly char separator;
        protected readonly uint maxColumnLength;

        protected CustomFieldsReportingGenerator(CustomField customField, char separator, uint maxColumnLength)
        {
            this.customField = customField;
            this.separator = separator;
            this.maxColumnLength = maxColumnLength;
        }
    }
}
