using System.Collections.Generic;

namespace Unity.Flex.Reporting.DataGenerators
{
    public interface IReportingDataGenerator
    {
        Dictionary<string, List<string>> Generate();
    }
}
