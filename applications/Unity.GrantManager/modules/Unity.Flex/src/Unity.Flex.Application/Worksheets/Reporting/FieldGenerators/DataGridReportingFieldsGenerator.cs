using System;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Worksheets.Reporting.FieldGenerators
{
    public class DataGridReportingFieldsGenerator(CustomField customField, char separator, uint maxColumnLength)
        : ReportingFieldsGeneratorBase(customField, separator, maxColumnLength), IReportingFieldsGenerator
    {
        public (string columns, string keys) Generate()
        {
            throw new NotImplementedException();
        }
    }
}
