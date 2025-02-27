using System.Text;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class CustomFieldsReportingGenerator : ReportingFieldsGenerator
    {
        protected readonly CustomField customField;
        protected StringBuilder keysString = new();
        protected StringBuilder columnsString = new();

        protected CustomFieldsReportingGenerator(CustomField customField)
        {
            this.customField = customField;
        }

        /// <summary>
        /// Trim any trailing delimeter characters and return the stringified keys and columns
        /// </summary>
        /// <param name="keysString"></param>
        /// <param name="columnsString"></param>
        /// <returns>A tuple with the formatted keys and columns for reporting fields</returns>
        protected static (string keys, string columns) TrimAndCreateKeysAndColumns(StringBuilder keysString, StringBuilder columnsString)
        {
            keysString.TrimEndDelimeter();
            columnsString.TrimEndDelimeter();

            return (keysString.ToString(), SanitizeColumnsString(columnsString));
        }

        private static string SanitizeColumnsString(StringBuilder columnsString)
        {
            var columnsToFormat = columnsString
                .ToString()
                .Split(ReportingConsts.ReportFieldDelimiter);

            // Iterate over the values and update each value
            for (int i = 0; i < columnsToFormat.Length; i++)
            {
                // Example update: append "_updated" to each value
                columnsToFormat[i] = SanitizeColumnName(columnsToFormat[i]);
            }

            return string.Join(ReportingConsts.ReportFieldDelimiter, columnsToFormat);
        }
    }
}
