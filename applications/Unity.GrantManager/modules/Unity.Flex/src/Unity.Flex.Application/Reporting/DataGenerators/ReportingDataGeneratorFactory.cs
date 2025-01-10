using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Reporting.DataGenerators
{
    public static class ReportingDataGeneratorFactory
    {
        public static IReportingDataGenerator Create(CustomField customField, FieldInstanceValue value)
        {
            return customField.Type switch
            {
                CustomFieldType.CheckboxGroup => new CustomFieldValueGenerators.CheckboxGroupReportDataGenerator(customField, value),
                CustomFieldType.DataGrid => new CustomFieldValueGenerators.DataGridReportDataGenerator(customField, value),
                _ => new CustomFieldValueGenerators.DefaultReportDataGenerator(customField, value),
            };
        }

        public static IReportingDataGenerator Create(Question question, Answer answer)
        {
            return new AnswerGenerators.DefaultReportDataGenerator(question, answer);
        }
    }
}
