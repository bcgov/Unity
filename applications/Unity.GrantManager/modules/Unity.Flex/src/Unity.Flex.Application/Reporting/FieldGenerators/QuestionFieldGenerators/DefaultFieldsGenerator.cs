using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.FieldGenerators.QuestionFieldGenerators
{
    public class DefaultFieldsGenerator(Question question, char separator, uint maxColumnLength)
        : QuestionsReportingGenerator(question, separator, maxColumnLength), IReportingFieldsGenerator
    {
        public (string columns, string keys) Generate()
        {
            throw new System.NotImplementedException();
        }
    }
}