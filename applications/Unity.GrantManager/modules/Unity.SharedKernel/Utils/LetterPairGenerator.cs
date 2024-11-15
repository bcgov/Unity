using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.Modules.Shared.Utils
{
    public partial class LetterPairGenerator
    {
        // Protected constructor to prevent direct instantiation
        protected LetterPairGenerator() { }

        private const int timeoutSeconds = 30; // Set the timeout (1 second for this example)

        // Generate letter pairs from a word
        private static IEnumerable<string> LetterPairs(string str)
        {
            int numPairs = str.Length - 1;
            for (int i = 0; i < numPairs; i++)
            {
                yield return str.Substring(i, 2);  // Generate pair of characters
            }
        }

        // Async method to split the string with a timeout using a CancellationToken
        private static string[] SplitWithTimeout(string str, int timeoutSeconds)
        {

            // Start a task to perform the Regex.Split asynchronously
#pragma warning disable SYSLIB1045
            var task = Regex.Split(str, @"\s+", RegexOptions.None, TimeSpan.FromSeconds(timeoutSeconds));
#pragma warning restore SYSLIB1045
            // Await the task result or cancel if timeout occurs
            return task;
        }

        // Public method to generate letter pairs from words, with timeout handling
        public static List<string> WordLetterPairs(string str)
        {
            var allPairs = new List<string>();

            // Await the split operation with timeout
            string[] words = SplitWithTimeout(str, timeoutSeconds);

            // Generate letter pairs for each word and add them to the list
            allPairs.AddRange(words.Where(word => !string.IsNullOrEmpty(word))  // Filter out empty words
                                   .SelectMany(LetterPairs));  // Generate pairs for each word

            return allPairs;
        }
    }
}
