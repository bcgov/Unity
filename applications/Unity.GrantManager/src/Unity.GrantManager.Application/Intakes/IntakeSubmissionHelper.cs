using System;
using System.Collections.Generic;

namespace Unity.GrantManager.Intakes
{
    public static class IntakeSubmissionHelper
    {
        /// <summary>
        /// Possible paths to search for the OIDC sub identifier in a submission object.
        /// Paths are checked in order until a non-empty value is found.
        /// Format: "property->nestedProperty->deeplyNestedProperty"
        /// </summary>
        private static readonly string[] SubSearchPaths =
        [
            "submission->data->applicantAgent->sub",
            "submission->data->hiddenApplicantAgent->sub",
            "createdBy"
        ];

        /// <summary>
        /// Extracts the OIDC sub identifier from a submission, excluding the IDP suffix (after @)
        /// Searches through configured paths until a value is found
        /// </summary>
        /// <param name="submission">The dynamic submission object from CHEFS</param>
        /// <returns>The normalized (uppercase) sub identifier, or Guid.Empty string if not found</returns>
        public static string ExtractOidcSub(dynamic submission)
        {
            try
            {
                string? sub = null;

                // Try each search path until we find a value
                foreach (var path in SubSearchPaths)
                {
                    sub = GetValueFromPath(submission, path);
                    if (!string.IsNullOrWhiteSpace(sub))
                    {
                        break;
                    }
                }

                if (string.IsNullOrWhiteSpace(sub))
                {
                    return Guid.Empty.ToString();
                }

                // Extract the identifier part before the @ symbol and convert to uppercase
                var atIndex = sub.IndexOf('@');
                if (atIndex == 0)
                {
                    // @ at the beginning means no identifier
                    return Guid.Empty.ToString();
                }
                
                if (atIndex > 0)
                {
                    return sub[..atIndex].ToUpper();
                }

                // No @ symbol found, return the whole sub uppercased
                return sub.ToUpper();
            }
            catch
            {
                return Guid.Empty.ToString();
            }
        }

        /// <summary>
        /// Traverses a dynamic object using a path string
        /// </summary>
        /// <param name="obj">The dynamic object to traverse</param>
        /// <param name="path">Path string with properties separated by "->"</param>
        /// <returns>The value as a string, or null if not found</returns>
        private static string? GetValueFromPath(dynamic obj, string path)
        {
            try
            {
                var properties = path.Split("->");
                dynamic? current = obj;

                foreach (var property in properties)
                {
                    if (current == null)
                    {
                        return null;
                    }

                    // Access the property dynamically
                    current = GetProperty(current, property);
                }

                return current?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a property from a dynamic object safely
        /// </summary>
        private static dynamic? GetProperty(dynamic obj, string propertyName)
        {
            try
            {
                // Try as dictionary first (works for ExpandoObject and similar types)
                if (obj is IDictionary<string, object> dictionary)
                {
                    return dictionary.TryGetValue(propertyName, out var value) ? value : null;
                }

                // Try reflection for regular objects
                var type = obj.GetType();
                var property = type.GetProperty(propertyName);
                
                if (property != null)
                {
                    return property.GetValue(obj);
                }

                // Try as indexer access
                return obj[propertyName];
            }
            catch
            {
                return null;
            }
        }
    }
}
