using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Reporting.DataGenerators.CustomFieldValueGenerators
{
    /// <summary>
    /// Custom Fields Report Data Generator Base
    /// </summary>
    public class ReportingDataGenerator
    {
        protected CustomField customField;
        protected FieldInstanceValue value;

        protected ReportingDataGenerator(CustomField customField, FieldInstanceValue value)
        {
            this.customField = customField;
            this.value = value;
        }
    }
}
