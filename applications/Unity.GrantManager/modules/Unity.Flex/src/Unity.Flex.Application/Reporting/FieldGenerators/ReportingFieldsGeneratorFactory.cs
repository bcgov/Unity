using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators;
using Unity.Flex.Reporting.FieldGenerators.QuestionFieldGenerators;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators
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

        public static IReportingFieldsGenerator Create(Question question, char separator, uint maxColumnLength)
        {
            return new DefaultFieldsGenerator(question, separator, maxColumnLength);
        }
    }
}
