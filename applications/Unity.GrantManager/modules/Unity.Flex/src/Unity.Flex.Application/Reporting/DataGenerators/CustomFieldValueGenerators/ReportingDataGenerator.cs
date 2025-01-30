using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.DataGenerators.CustomFieldValueGenerators
{
    /// <summary>
    /// Custom Fields Report Data Generator Base
    /// </summary>
    public class ReportingDataGenerator
    {
        protected CustomField customField;
        protected CustomFieldValue value;

        protected ReportingDataGenerator(CustomField customField, CustomFieldValue value)
        {
            this.customField = customField;
            this.value = value;
        }
    }
}
