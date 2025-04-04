using System.Collections.Generic;

namespace Unity.Flex.Reporting.DataGenerators
{
    public interface IReportingDataGenerator
    {
        // Return an array of values matched to the key and a flag to indicate array handling
        (Dictionary<string, List<string>> keyValuePairs, bool compressArray) Generate();
    }
}
