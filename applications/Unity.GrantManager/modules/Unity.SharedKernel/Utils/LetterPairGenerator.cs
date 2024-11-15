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
        
        private const int timeoutMilliseconds = 1000; // Set the timeout (1 second for this example)

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
        private static async Task<string[]> SplitWithTimeoutAsync(string str, int timeoutMilliseconds)
        {
            using (var cts = new CancellationTokenSource(timeoutMilliseconds))
            {
                // Start a task to perform the Regex.Split asynchronously
                #pragma warning disable SYSLIB1045
                var task = Task.Run(() => Regex.Split(str, @"\s+"), cts.Token);
                #pragma warning restore SYSLIB1045
                // Await the task result or cancel if timeout occurs
                return await task;
            }
        }

        // Public method to generate letter pairs from words, with timeout handling
        public static async Task<List<string>> WordLetterPairs(string str)
        {
            var allPairs = new List<string>();

            // Await the split operation with timeout
            string[] words = await SplitWithTimeoutAsync(str, timeoutMilliseconds);

            // Generate letter pairs for each word and add them to the list
            allPairs.AddRange(words.Where(word => !string.IsNullOrEmpty(word))  // Filter out empty words
                                   .SelectMany(LetterPairs));  // Generate pairs for each word

            return allPairs;
        }
    }
}
