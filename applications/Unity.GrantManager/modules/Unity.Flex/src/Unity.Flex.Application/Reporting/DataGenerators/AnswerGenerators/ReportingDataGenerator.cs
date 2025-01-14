using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.DataGenerators.AnswerGenerators
{
    public class ReportingDataGenerator
    {
        protected Answer answer;
        protected ReportingDataGenerator(Answer answer)
        {
            this.answer = answer;
        }
    }
}
