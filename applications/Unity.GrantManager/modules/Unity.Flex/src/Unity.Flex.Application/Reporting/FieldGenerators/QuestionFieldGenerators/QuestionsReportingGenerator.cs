using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.FieldGenerators.QuestionFieldGenerators
{
    public class QuestionsReportingGenerator
    {
        protected readonly Question question;
        protected readonly char separator;
        protected readonly uint maxColumnLength;

        protected QuestionsReportingGenerator(Question question, char separator, uint maxColumnLength)
        {
            this.question = question;
            this.separator = separator;
            this.maxColumnLength = maxColumnLength;
        }
    }
}
