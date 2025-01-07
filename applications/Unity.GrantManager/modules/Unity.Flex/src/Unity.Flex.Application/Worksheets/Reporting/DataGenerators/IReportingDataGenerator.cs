using System.Collections.Generic;

namespace Unity.Flex.Worksheets.Reporting.DataGenerators
{
    public interface IReportingDataGenerator
    {
        Dictionary<string, List<string>> Generate();
    }
}
