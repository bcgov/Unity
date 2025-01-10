using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators;
using Unity.Flex.Reporting.FieldGenerators.QuestionFieldGenerators;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators
{
    public static class ReportingFieldsGeneratorFactory
    {
        public static IReportingFieldsGenerator Create(CustomField customField)
        {
            return customField.Type switch
            {
                CustomFieldType.CheckboxGroup => new CheckboxGroupReportingFieldsGenerator(customField),
                CustomFieldType.DataGrid => new DataGridReportingFieldsGenerator(customField),
                _ => new DefaultReportingFieldsGenerator(customField),
            };
        }

        public static IReportingFieldsGenerator Create(Question question)
        {
            return new DefaultFieldsGenerator(question);
        }
    }
}
