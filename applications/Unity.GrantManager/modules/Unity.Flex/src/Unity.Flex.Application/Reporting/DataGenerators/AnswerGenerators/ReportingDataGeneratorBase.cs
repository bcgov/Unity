using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.DataGenerators.AnswerGenerators
{
    public class ReportingDataGeneratorBase
    {
        protected Question question;
        protected Answer answer;

        protected ReportingDataGeneratorBase(Question question, Answer answer)
        {
            this.question = question;
            this.answer = answer;
        }
    }
}
