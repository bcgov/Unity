using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Worksheets.Reporting
{
    public class ReportingFieldsGeneratorBase
    {
        protected readonly CustomField customField;
        protected readonly char separator;
        protected readonly uint maxColumnLength;

        protected ReportingFieldsGeneratorBase(CustomField customField, char separator, uint maxColumnLength)
        {
            this.customField = customField;
            this.separator = separator;
            this.maxColumnLength = maxColumnLength;
        }
    }
}
