using System;

namespace Unity.GrantManager.AI
{
    internal static class AIResponseJson
    {
        public static string CleanJsonResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return string.Empty;
            }

            var cleaned = response.Trim();

            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase) || cleaned.StartsWith("```"))
            {
                var startIndex = cleaned.IndexOf('\n');
                if (startIndex >= 0)
                {
                    cleaned = cleaned[(startIndex + 1)..];
                }
                else
                {
                    var jsonStart = FindFirstJsonTokenIndex(cleaned);
                    if (jsonStart > 0)
                    {
                        cleaned = cleaned[jsonStart..];
                    }
                }
            }

            if (cleaned.EndsWith("```", StringComparison.Ordinal))
            {
                var lastIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
                if (lastIndex > 0)
                {
                    cleaned = cleaned[..lastIndex];
                }
            }

            return cleaned.Trim();
        }

        private static int FindFirstJsonTokenIndex(string value)
        {
            var objectStart = value.IndexOf('{');
            var arrayStart = value.IndexOf('[');

            if (objectStart >= 0 && arrayStart >= 0)
            {
                return Math.Min(objectStart, arrayStart);
            }

            if (objectStart >= 0)
            {
                return objectStart;
            }

            return arrayStart;
        }
    }
}
