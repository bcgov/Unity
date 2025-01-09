using System;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators
{
    public class CheckboxGroupReportingFieldsGenerator(CustomField customField, char separator, uint maxColumnLength)
        : ReportingFieldsGeneratorBase(customField, separator, maxColumnLength), IReportingFieldsGenerator
    {
        public (string columns, string keys) Generate()
        {
            throw new NotImplementedException();
        }
    }
}
