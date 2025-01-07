using System;
using System.Collections.Generic;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Worksheets.Reporting.DataGenerators
{
    public class CheckboxGroupReportDataGenerator(CustomField customField, FieldInstanceValue value)
        : ReportingDataGeneratorBase(customField, value), IReportingDataGenerator
    {
        public Dictionary<string, List<string>> Generate()
        {
            throw new NotImplementedException();
        }
    }
}
