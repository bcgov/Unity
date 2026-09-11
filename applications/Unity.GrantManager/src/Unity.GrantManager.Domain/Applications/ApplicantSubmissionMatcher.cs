using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;

namespace Unity.GrantManager.Applications;

/// <inheritdoc cref="IApplicantSubmissionMatcher" />
public class ApplicantSubmissionMatcher : DomainService, IApplicantSubmissionMatcher
{
    /// <inheritdoc />
    public virtual async Task<List<Guid>> ResolveApplicantIdsAsync(IQueryable<ApplicationFormSubmission> submissionsQuery, string normalizedSubject)
    {
        return await AsyncExecuter.ToListAsync(submissionsQuery
            .Where(s => s.OidcSub == normalizedSubject && s.ApplicantId != Guid.Empty)
            .Select(s => s.ApplicantId)
            .Distinct());
    }

    /// <inheritdoc />
    public virtual async Task<IQueryable<ApplicationFormSubmission>> GetMatchingSubmissionsAsync(IQueryable<ApplicationFormSubmission> submissionsQuery, string normalizedSubject)
    {
        var applicantIds = await ResolveApplicantIdsAsync(submissionsQuery, normalizedSubject);

        return submissionsQuery.Where(s => s.OidcSub == normalizedSubject || applicantIds.Contains(s.ApplicantId));
    }
}
