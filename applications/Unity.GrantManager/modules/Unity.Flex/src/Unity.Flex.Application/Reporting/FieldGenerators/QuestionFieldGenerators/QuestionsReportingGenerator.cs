using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.FieldGenerators.QuestionFieldGenerators
{
    public class QuestionsReportingGenerator : ReportingFieldsGenerator
    {
        protected readonly Question question;

        protected QuestionsReportingGenerator(Question question)
        {
            this.question = question;
        }
    }
}
