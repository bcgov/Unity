using System;

namespace Unity.Modules.Shared.Utils
{
    public static class StringExtensions
    {

        public static string RemoveNewLines(this string inputString)
        {
            return inputString.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
        }
    }
}
