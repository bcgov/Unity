using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Domain.Entities;
using Xunit;

namespace Unity.GrantManager.Applications
{
    public class ApplicantSubmissionMatcherTests : GrantManagerDomainTestBase
    {
        private readonly IApplicantSubmissionMatcher _matcher;

        public ApplicantSubmissionMatcherTests()
        {
            _matcher = GetRequiredService<IApplicantSubmissionMatcher>();
        }

        private static ApplicationFormSubmission CreateSubmission(string oidcSub, Guid applicantId)
        {
            var entity = new ApplicationFormSubmission
            {
                OidcSub = oidcSub,
                ApplicantId = applicantId,
                ApplicationId = Guid.NewGuid()
            };
            EntityHelper.TrySetId(entity, () => Guid.NewGuid());
            return entity;
        }

        [Fact]
        public async Task ResolveApplicantIdsAsync_ShouldReturnDistinctApplicantIds_ForMatchingSubject()
        {
            var applicantId = Guid.NewGuid();
            var submissions = new[]
            {
                CreateSubmission("TESTUSER", applicantId),
                CreateSubmission("TESTUSER", applicantId),
                CreateSubmission("OTHERUSER", Guid.NewGuid())
            }.AsQueryable();

            var result = await _matcher.ResolveApplicantIdsAsync(submissions, "TESTUSER");

            result.ShouldBe([applicantId]);
        }

        [Fact]
        public async Task ResolveApplicantIdsAsync_ShouldExcludeEmptyApplicantId()
        {
            var submissions = new[]
            {
                CreateSubmission("TESTUSER", Guid.Empty)
            }.AsQueryable();

            var result = await _matcher.ResolveApplicantIdsAsync(submissions, "TESTUSER");

            result.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetMatchingSubmissionsAsync_ShouldIncludeSubmissions_ForSameApplicantUnderDifferentOidcSub()
        {
            var applicantId = Guid.NewGuid();
            var directSubmission = CreateSubmission("TESTUSER", applicantId);
            var crossLoginSubmission = CreateSubmission("OTHERUSER", applicantId);
            var unrelatedSubmission = CreateSubmission("OTHERUSER", Guid.NewGuid());

            var submissions = new[] { directSubmission, crossLoginSubmission, unrelatedSubmission }.AsQueryable();

            var result = (await _matcher.GetMatchingSubmissionsAsync(submissions, "TESTUSER")).ToList();

            result.Count.ShouldBe(2);
            result.ShouldContain(directSubmission);
            result.ShouldContain(crossLoginSubmission);
            result.ShouldNotContain(unrelatedSubmission);
        }

        [Fact]
        public async Task GetMatchingSubmissionsAsync_WithNoDirectMatches_ShouldReturnEmpty()
        {
            var submissions = new[]
            {
                CreateSubmission("OTHERUSER", Guid.NewGuid())
            }.AsQueryable();

            var result = (await _matcher.GetMatchingSubmissionsAsync(submissions, "TESTUSER")).ToList();

            result.ShouldBeEmpty();
        }
    }
}
