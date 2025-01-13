using System.Collections.Generic;
using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.DataGenerators.AnswerGenerators
{
    public class DefaultReportDataGenerator(Answer answer)
       : ReportingDataGeneratorBase(answer), IReportingDataGenerator
    {
        public Dictionary<string, List<string>> Generate()
        {
            return new Dictionary<string, List<string>>
            {
                {
                    answer.Question!.Name, new List<string>()
                    {
                        ValueResolver.Resolve(answer.CurrentValue ?? string.Empty, answer.Question!.Type)?.ToString() ?? string.Empty
                    }
                }
            };
        }
    }
}
