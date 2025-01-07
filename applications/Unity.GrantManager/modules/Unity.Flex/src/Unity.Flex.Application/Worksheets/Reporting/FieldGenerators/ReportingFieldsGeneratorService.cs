using System.Linq;
using System.Text;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Worksheets.Reporting.FieldGenerators
{
    public static class ReportingFieldsGeneratorService
    {
        public static Worksheet GenerateAndSet(Worksheet worksheet, char separator = '|', uint maxColumnLength = 63)
        {
            var (reportingKeys, reportingColumns, reportViewName) = GenerateReportingFields(worksheet, separator, maxColumnLength);
            worksheet.SetReportingFields(reportingKeys, reportingColumns, reportViewName);
            return worksheet;
        }

        private static (string reportingKeys, string reportingColumns, string reportViewName) GenerateReportingFields(Worksheet worksheet,
            char separator,
            uint maxColumnLength)
        {
            StringBuilder keysBuilder = new();
            StringBuilder columnsBuilder = new();

            foreach (var field in worksheet.Sections.SelectMany(s => s.Fields))
            {
                var (columns, keys) = ReportingFieldsGeneratorFactory
                                        .Create(field, separator, maxColumnLength)
                                        .Generate();

                columnsBuilder.Append(columns).Append(separator);
                keysBuilder.Append(keys).Append(separator);
            }

            // Remove the trailing separator
            if (columnsBuilder.Length > 0)
            {
                columnsBuilder.Length--;
                // Remove the last separator
            }

            if (keysBuilder.Length > 0)
            {
                keysBuilder.Length--; // Remove the last separator                                    
            }

            return new(columnsBuilder.ToString(), keysBuilder.ToString(), $"WS-{worksheet.Name.ToUpper()}");
        }
    }
}
