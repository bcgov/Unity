using System.Text.RegularExpressions;

namespace Unity.Flex.Domain.Utils
{
    public static partial class SheetParserFunctions
    {
        public static string[] SplitSheetNameAndVersion(string name)
        {
            var versionIndicator = name.LastIndexOf('-');
            return [name[0..versionIndicator], name[versionIndicator..]];
        }

        public static string RemoveTrailingNumbers(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return TrailingZeroes().Replace(input, string.Empty);
        }


        [GeneratedRegex(@"\d+$")]
        private static partial Regex TrailingZeroes();
    }
}
