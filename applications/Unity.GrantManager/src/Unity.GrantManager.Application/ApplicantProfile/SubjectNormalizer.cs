namespace Unity.GrantManager.ApplicantProfile
{
    /// <summary>
    /// Centralises OIDC subject normalization used by applicant-profile providers.
    /// Strips the domain portion after '@' and upper-cases the result.
    /// Returns <c>null</c> when the input is null, empty, or whitespace so
    /// callers can short-circuit before hitting the database.
    /// </summary>
    public static class SubjectNormalizer
    {
        public static string? Normalize(string? subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return null;

            var atIndex = subject.IndexOf('@');

            if (atIndex == 0)
                return null;

            return atIndex > 0
                ? subject[..atIndex].ToUpperInvariant()
                : subject.ToUpperInvariant();
        }
    }
}
