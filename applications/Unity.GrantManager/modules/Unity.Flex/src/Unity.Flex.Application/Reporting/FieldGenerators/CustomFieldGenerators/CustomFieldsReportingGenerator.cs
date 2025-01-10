using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class CustomFieldsReportingGenerator
    {
        protected readonly CustomField customField;

        protected CustomFieldsReportingGenerator(CustomField customField)
        {
            this.customField = customField;
        }
    }
}
