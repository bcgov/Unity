using System.Collections.Generic;
using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.DataGenerators.AnswerGenerators
{
    public class DefaultReportDataGenerator(Question question, Answer answer)
       : ReportingDataGeneratorBase(question, answer), IReportingDataGenerator
    {
        public Dictionary<string, List<string>> Generate()
        {
            throw new System.NotImplementedException();
        }
    }
}
