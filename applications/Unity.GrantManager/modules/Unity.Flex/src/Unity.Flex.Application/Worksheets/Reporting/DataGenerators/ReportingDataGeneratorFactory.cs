using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Worksheets.Reporting.DataGenerators
{
    public static class ReportingDataGeneratorFactory
    {
        public static IReportingDataGenerator Create(CustomField customField, FieldInstanceValue value)
        {
            return customField.Type switch
            {
                CustomFieldType.CheckboxGroup => new CheckboxGroupReportDataGenerator(customField, value),
                CustomFieldType.DataGrid => new DataGridReportDataGenerator(customField, value),
                _ => new DefaultReportDataGenerator(customField, value),
            };
        }
    }
}
