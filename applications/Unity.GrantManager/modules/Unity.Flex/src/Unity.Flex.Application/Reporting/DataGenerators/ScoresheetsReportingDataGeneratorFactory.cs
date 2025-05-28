using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.DataGenerators
{
    public static class ScoresheetsReportingDataGeneratorFactory
    {
        /// <summary>
        /// Returns the correct Answer Type Data Generator based on the Answer
        /// </summary>
        /// <param name="answer"></param>
        /// <returns>Relevant IReportingDataGenerator that can generate the ReportingData field relevant to the type</returns>
        public static IReportingDataGenerator Create(Answer answer)
        {
            return new AnswerGenerators.DefaultReportDataGenerator(answer);
        }
    }
}
