using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Unity.Modules.Shared.Utils
{
    public partial class LetterPairGenerator
    {

        // Protected constructor to prevent direct instantiation
        protected LetterPairGenerator() { }
  
        // Generate letter pairs from a word
        private static IEnumerable<string> LetterPairs(string str)
        {
            int numPairs = str.Length - 1;
            for (int i = 0; i < numPairs; i++)
            {
                yield return str.Substring(i, 2);  // Generate pair of characters
            }
        }

        public static List<string> WordLetterPairs(string str)
        {
            var allPairs = new List<string>();
            #pragma warning disable SYSLIB1045
            string[] words = Regex.Split(str, @"\s+");
            #pragma warning restore SYSLIB1045
            // Generate letter pairs for each word and add them to the list
            allPairs.AddRange(words.Where(word => !string.IsNullOrEmpty(word))  // Filter out empty words
                                   .SelectMany(LetterPairs));  // Generate pairs for each word

            return allPairs;
        }
    }
}
