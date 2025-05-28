using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class DefaultReportingFieldsGenerator(CustomField customField)
        : CustomFieldsReportingGenerator(customField), IReportingFieldsGenerator
    {
        /// <summary>
        /// Generate the default reporting fields keys and columns for a custom field
        /// </summary>
        /// <returns>A tuple with the formatted keys and columns for reporting fields</returns>
        public (string keys, string columns) Generate()
        {
            // We intentionally use the key twice here
            return (customField.Key, SanitizeColumnName(customField.Key));
        }
    }
}
