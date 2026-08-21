using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation
{
    /// <summary>
    /// AI scoring results moved off the Application aggregate into AI.ApplicationScoresheetAnswers.
    /// These cover the upsert behaviour that replaced the old direct column assignment.
    /// </summary>
    public class ApplicationScoresheetAnswersPersistenceTests : GrantManagerApplicationTestBase
    {
        private readonly IRepository<ApplicationScoresheetAnswers, Guid> _scoresheetAnswersRepository;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IGuidGenerator _guidGenerator;

        public ApplicationScoresheetAnswersPersistenceTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            _scoresheetAnswersRepository = GetRequiredService<IRepository<ApplicationScoresheetAnswers, Guid>>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
            _guidGenerator = GetRequiredService<IGuidGenerator>();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Should_InsertAnswers_When_NoneStoredForApplication()
        {
            var applicationId = _guidGenerator.Create();
            const string answers = """{"11111111-1111-1111-1111-111111111111":{"answer":"3"}}""";

            await AIGenerationRequestJobHelper.SaveScoresheetAnswersInNewUowAsync(
                _unitOfWorkManager, _scoresheetAnswersRepository, _guidGenerator, applicationId, answers);

            using var uow = _unitOfWorkManager.Begin();
            var stored = await _scoresheetAnswersRepository.FindAsync(x => x.ApplicationId == applicationId);

            stored.ShouldNotBeNull();
            stored.Answers.ShouldBe(answers);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task Should_UpdateExistingRow_When_ScoringRegeneratedForSameApplication()
        {
            var applicationId = _guidGenerator.Create();
            const string firstAnswers = """{"11111111-1111-1111-1111-111111111111":{"answer":"3"}}""";
            const string secondAnswers = """{"11111111-1111-1111-1111-111111111111":{"answer":"5"}}""";

            await AIGenerationRequestJobHelper.SaveScoresheetAnswersInNewUowAsync(
                _unitOfWorkManager, _scoresheetAnswersRepository, _guidGenerator, applicationId, firstAnswers);
            await AIGenerationRequestJobHelper.SaveScoresheetAnswersInNewUowAsync(
                _unitOfWorkManager, _scoresheetAnswersRepository, _guidGenerator, applicationId, secondAnswers);

            using var uow = _unitOfWorkManager.Begin();
            var rows = (await _scoresheetAnswersRepository.GetQueryableAsync())
                .Where(x => x.ApplicationId == applicationId)
                .ToList();

            rows.Count.ShouldBe(1);
            rows[0].Answers.ShouldBe(secondAnswers);
        }
    }
}
