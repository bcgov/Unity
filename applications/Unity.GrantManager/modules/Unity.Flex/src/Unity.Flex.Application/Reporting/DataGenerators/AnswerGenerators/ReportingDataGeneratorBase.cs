using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.DataGenerators.AnswerGenerators
{
    public class ReportingDataGeneratorBase
    {
        protected Answer answer;
        protected ReportingDataGeneratorBase(Answer answer)
        {
            this.answer = answer;
        }
    }
}
