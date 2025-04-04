using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Reporting.DataGenerators
{
    public class ReportingDataGeneratorServiceBase : ApplicationService
    {
        protected static void ExtractKeyValueData(Dictionary<string, object?> reportData,
            (Dictionary<string, List<string>> keyValuePairs, bool compressArray) keyValues)
        {
            var compressArray = keyValues.compressArray;

            foreach (var keyValue in from keyValue in keyValues.keyValuePairs
                                     where reportData.ContainsKey(keyValue.Key)
                                     select keyValue)
            {
                if (compressArray)
                {
                    reportData[keyValue.Key] = keyValue.Value[0];
                }
                else
                {
                    reportData[keyValue.Key] = keyValue.Value;
                }
            }
        }
    }
}
