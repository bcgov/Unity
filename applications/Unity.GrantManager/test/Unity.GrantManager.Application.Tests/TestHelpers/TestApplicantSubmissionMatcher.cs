using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;

namespace Unity.GrantManager.TestHelpers
{
    /// <summary>
    /// Plain-LINQ stand-in for <see cref="ApplicantSubmissionMatcher"/> used by provider unit tests.
    /// The real implementation lives in the Domain layer and relies on ABP's <c>AsyncExecuter</c>,
    /// which requires a DI container; these tests construct providers directly with NSubstitute
    /// repositories, so this fake reproduces the same matching behaviour without that dependency.
    /// </summary>
    internal class TestApplicantSubmissionMatcher : IApplicantSubmissionMatcher
    {
        public async Task<List<Guid>> ResolveApplicantIdsAsync(IQueryable<ApplicationFormSubmission> submissionsQuery, string normalizedSubject)
        {
            return await submissionsQuery
                .Where(s => s.OidcSub == normalizedSubject && s.ApplicantId != Guid.Empty)
                .Select(s => s.ApplicantId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<IQueryable<ApplicationFormSubmission>> GetMatchingSubmissionsAsync(IQueryable<ApplicationFormSubmission> submissionsQuery, string normalizedSubject)
        {
            var applicantIds = await ResolveApplicantIdsAsync(submissionsQuery, normalizedSubject);

            return submissionsQuery.Where(s => s.OidcSub == normalizedSubject || applicantIds.Contains(s.ApplicantId));
        }
    }
}
