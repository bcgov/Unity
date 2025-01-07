using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Worksheets.Reporting.DataGenerators
{
    public class ReportingDataGeneratorBase
    {
        protected CustomField customField;
        protected FieldInstanceValue value;

        protected ReportingDataGeneratorBase(CustomField customField, FieldInstanceValue value)
        {
            this.customField = customField;
            this.value = value;
        }
    }
}
