using System.Text;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class CustomFieldsReportingGenerator
    {
        protected readonly CustomField customField;
        protected StringBuilder keysString = new();
        protected StringBuilder columnsString = new();

        protected CustomFieldsReportingGenerator(CustomField customField)
        {
            this.customField = customField;
        }

        protected static (string keys, string columns) TrimAndCreateKeysAndColumns(StringBuilder keysString, StringBuilder columnsString)
        {
            keysString.TrimEndDelimeter();
            columnsString.TrimEndDelimeter();

            return (keysString.ToString(), columnsString.ToString());
        }
    }
}
