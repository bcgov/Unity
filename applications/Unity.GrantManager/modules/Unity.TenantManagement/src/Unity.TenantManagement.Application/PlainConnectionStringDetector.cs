using System.Text.RegularExpressions;

namespace Unity.TenantManagement.Application
{
    internal static partial class PlainConnectionStringDetector
    {
        // Require at least 2 keyword hits - a single hit (e.g. "Pwd=" or "Uid=") could
        // coincidentally occur at the tail of valid base64 ciphertext, right before the
        // padding '='. Real connection strings always carry several key=value pairs.
        private const int MinKeywordMatches = 2;

        public static bool LooksLikePlainConnectionString(string value)
            => ConnectionStringKeywordPattern().Matches(value).Count >= MinKeywordMatches;

        [GeneratedRegex(@"(Host|Server|Port|Database|Initial Catalog|Username|User Id|Uid|Pwd|Password|Data Source)\s*=",
            RegexOptions.IgnoreCase)]
        private static partial Regex ConnectionStringKeywordPattern();
    }
}
