using System;
using Unity.Flex.Domain.Worksheets;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class CheckboxGroupReportingFieldsGenerator(CustomField customField)
        : CustomFieldsReportingGenerator(customField), IReportingFieldsGenerator
    {
        public (string columns, string keys) Generate()
        {
            throw new NotImplementedException();
        }
    }
}
