using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Modules.Shared.Utils
{
    public static class StringExtensions
    {

        public static string RemoveNewLines(this string inputString)
        {
            return inputString.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        }

        public static string SanitizeField(this string inputString)
        {
            return inputString.RemoveNewLines();
        }

        public static double CompareStrings(this string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
                return 0.0;

            var pairs1 = LetterPairGenerator.WordLetterPairs(str1.ToUpper());
            var pairs2 = new HashSet<string>(LetterPairGenerator.WordLetterPairs(str2.ToUpper()));

            // Calculate intersection using LINQ
            int intersection = pairs1.Count(pairs2.Remove);
            int union = pairs1.Count + pairs2.Count;
            
            double percentage = union == 0 ? 0.0 : 2.0 * intersection * 100 / union;
            return Math.Round(Math.Min(percentage, 100.0), 2); // Ensure it does not exceed 100% round to 2 decimals
        }
    }
}
