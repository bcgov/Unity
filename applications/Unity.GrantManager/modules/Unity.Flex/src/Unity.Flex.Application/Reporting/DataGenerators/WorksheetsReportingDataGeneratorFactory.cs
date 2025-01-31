using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Reporting.DataGenerators
{
    public static class WorksheetsReportingDataGeneratorFactory
    {
        /// <summary>
        /// Returns the correct Field Type Data Generator based on the Custom Field Type
        /// </summary>
        /// <param name="customField"></param>
        /// <param name="value"></param>
        /// <returns>Relevant IReportingDataGenerator that can generate the ReportingData field relevant to the type</returns>
        public static IReportingDataGenerator Create(CustomField customField, CustomFieldValue value)
        {
            return customField.Type switch
            {
                CustomFieldType.CheckboxGroup => new CustomFieldValueGenerators.CheckboxGroupReportDataGenerator(customField, value),
                CustomFieldType.DataGrid => new CustomFieldValueGenerators.DataGridReportDataGenerator(customField, value),
                _ => new CustomFieldValueGenerators.DefaultReportDataGenerator(customField, value),
            };
        }
    }
}
