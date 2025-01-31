using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.DataGenerators.AnswerGenerators
{
    /// <summary>
    /// Answer Report Data Generator Base
    /// </summary>
    public class ReportingDataGenerator
    {
        protected Answer answer;
        protected ReportingDataGenerator(Answer answer)
        {
            this.answer = answer;
        }
    }
}
