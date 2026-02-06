using System;

namespace Unity.GrantManager.Intakes
{
    public static class IntakeSubmissionHelper
    {
        /// <summary>
        /// Extracts the OIDC sub identifier from a submission, excluding the IDP suffix (after @)
        /// Tries hiddenApplicantAgent.sub first, falls back to createdBy field
        /// </summary>
        /// <param name="submission">The dynamic submission object from CHEFS</param>
        /// <returns>The normalized (uppercase) sub identifier, or Guid.Empty string if not found</returns>
        public static string ExtractOidcSub(dynamic submission)
        {
            try
            {
                // Try to get from hiddenApplicantAgent.sub first
                string? sub = submission?
                    .submission?
                    .data?
                    .applicantAgent?
                    .sub?
                    .ToString();
                
                if (string.IsNullOrWhiteSpace(sub))
                {
                    // Fall back to createdBy field
                    sub = submission?.createdBy?.ToString();
                }
                
                if (string.IsNullOrWhiteSpace(sub))
                {
                    return Guid.Empty.ToString();
                }

                // Extract the identifier part before the @ symbol and convert to uppercase
                var atIndex = sub.IndexOf('@');
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
    }
}
