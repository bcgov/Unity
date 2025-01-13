using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.FieldGenerators.QuestionFieldGenerators
{
    public class DefaultFieldsGenerator(Question question)
        : QuestionsReportingGenerator(question), IReportingFieldsGenerator
    {
        public (string columns, string keys) Generate()
        {
            return (SanitizeColumnName(question.Name), question.Name);
        }
    }
}