using System.Text;

namespace Unity.Flex.Reporting
{
    public static class ReportingExtensions
    {
        public static void TrimEndDelimeter(this StringBuilder stringBuilder)
        {
            // Remove the trailing separator
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Length--;
                // Remove the last separator
            }
        }

        public static StringBuilder AddFieldAndDelimiter(this StringBuilder stringBuilder, string field)
        {
            return stringBuilder
                   .Append(field)
                   .Append(ReportingConsts.ReportFieldDelimiter);
        }
    }
}
