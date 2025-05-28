using System.Collections.Generic;
using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.DataGenerators.AnswerGenerators
{
    public class DefaultReportDataGenerator(Answer answer)
       : ReportingDataGenerator(answer), IReportingDataGenerator
    {
        /// <summary>
        /// Default key values pairing for the reporting data generation
        /// </summary>
        /// <returns>Dictionary with keys and matched values for reporting data</returns>
        public (Dictionary<string, List<string>> keyValuePairs, bool compressArray) Generate()
        {
            var keyValues = new Dictionary<string, List<string>>
            {
                {
                    answer.Question!.Name, new List<string>()
                    {
                        ValueResolver.Resolve(answer.CurrentValue ?? string.Empty, answer.Question!.Type)?.ToString() ?? string.Empty
                    }
                }
            };

            return (keyValues, true);
        }
    }
}
