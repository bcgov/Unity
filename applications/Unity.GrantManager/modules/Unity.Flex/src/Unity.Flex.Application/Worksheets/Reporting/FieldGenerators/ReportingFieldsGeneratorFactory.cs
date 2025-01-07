using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Worksheets.Reporting.FieldGenerators
{
    public static class ReportingFieldsGeneratorFactory
    {
        public static IReportingFieldsGenerator Create(CustomField customField, char separator, uint maxColumnLength)
        {
            return customField.Type switch
            {
                CustomFieldType.CheckboxGroup => new CheckboxGroupReportingFieldsGenerator(customField, separator, maxColumnLength),
                CustomFieldType.DataGrid => new DataGridReportingFieldsGenerator(customField, separator, maxColumnLength),
                _ => new DefaultReportingFieldsGenerator(customField, separator, maxColumnLength),
            };
        }
    }
}
