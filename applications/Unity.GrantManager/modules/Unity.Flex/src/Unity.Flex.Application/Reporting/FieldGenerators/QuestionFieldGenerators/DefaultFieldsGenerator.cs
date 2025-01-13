using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.FieldGenerators.QuestionFieldGenerators
{
    public class DefaultFieldsGenerator(Question question)
        : QuestionsReportingGenerator(question), IReportingFieldsGenerator
    {
        public (string keys, string columns) Generate()
        {
            return (question.Name, SanitizeColumnName(question.Name));
        }
    }
}