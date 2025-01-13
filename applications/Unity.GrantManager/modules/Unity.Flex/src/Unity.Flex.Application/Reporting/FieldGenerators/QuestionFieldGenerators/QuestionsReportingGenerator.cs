using System.Text.RegularExpressions;
using Unity.Flex.Domain.Scoresheets;

namespace Unity.Flex.Reporting.FieldGenerators.QuestionFieldGenerators
{
    public partial class QuestionsReportingGenerator
    {
        protected readonly Question question;

        protected QuestionsReportingGenerator(Question question)
        {
            this.question = question;
        }

        protected static string SanitizeColumnName(string input)
        {
            // Remove invalid characters (keep only letters, numbers, and underscores)
            string sanitized = ValidColumnName().Replace(input, "");

            // Truncate to 63 characters
            if (sanitized.Length > ReportingConsts.ReportColumnMaxLength)
            {
                sanitized = sanitized[..ReportingConsts.ReportColumnMaxLength];
            }

            // Ensure the column name doesn't start with a number
            if (char.IsDigit(sanitized[0]))
            {
                sanitized = "_" + sanitized;
            }

            return sanitized;
        }

        [GeneratedRegex(@"[^a-zA-Z0-9_]")]
        private static partial Regex ValidColumnName();
    }
}
