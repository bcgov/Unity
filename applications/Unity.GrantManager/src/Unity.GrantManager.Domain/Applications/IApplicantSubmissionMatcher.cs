using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Unity.GrantManager.Applications;

/// <summary>
/// Expands an OIDC-subject-scoped view of <see cref="ApplicationFormSubmission"/> records to
/// also include submissions made under a different OIDC subject but linked to the same
/// applicant, so a single applicant who has used more than one login method still sees all
/// of their data.
/// </summary>
public interface IApplicantSubmissionMatcher
{
    /// <summary>
    /// Resolves the distinct applicant IDs linked to the given subject's own (OIDC-subject-matched)
    /// submissions.
    /// </summary>
    Task<List<Guid>> ResolveApplicantIdsAsync(IQueryable<ApplicationFormSubmission> submissionsQuery, string normalizedSubject);

    /// <summary>
    /// Returns the subject's own submissions unioned with any other submissions that share one
    /// of those applicant IDs (i.e. the same applicant via a different login/OIDC subject).
    /// </summary>
    Task<IQueryable<ApplicationFormSubmission>> GetMatchingSubmissionsAsync(IQueryable<ApplicationFormSubmission> submissionsQuery, string normalizedSubject);
}
