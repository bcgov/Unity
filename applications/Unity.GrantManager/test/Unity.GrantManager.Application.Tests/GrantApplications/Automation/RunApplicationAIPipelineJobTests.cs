using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Operations;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Unity.AI.RateLimit;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Features;
using Volo.Abp.MultiTenancy;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Uow;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.GrantApplications.Automation;

public class RunApplicationAIPipelineJobTests(ITestOutputHelper outputHelper) : GrantManagerApplicationTestBase(outputHelper)
{
    [Fact]
    public async Task ExecuteAsync_Should_Mark_Request_Completed_When_Features_Disabled()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(Arg.Any<string>()).Returns(false);

        var repository = BuildRequestRepository(out var requests);
        var request = CreateRequest();
        requests.Add(request);

        var job = BuildJob(featureChecker, repository);
        var requestedByUserId = Guid.NewGuid();

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = request.ApplicationId!.Value,
            RequestKey = request.RequestKey,
            RequestedByUserId = requestedByUserId,
            TenantId = request.TenantId
        });

        request.Status.ShouldBe(AIGenerationRequestStatus.Completed);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Publish_Event_When_Pipeline_Scoring_Completes()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);

        var repository = BuildRequestRepository(out var requests);
        var request = CreateRequest();
        requests.Add(request);

        var scoringService = Substitute.For<IApplicationScoringService>();
        scoringService.RegenerateAsync(Arg.Any<ApplicationScoringOperationInputDto>(), Arg.Any<CancellationToken>())
            .Returns("{}");
        var inputBuilder = Substitute.For<IAIApplicationInputBuilder>();
        inputBuilder.BuildApplicationScoringInputAsync(Arg.Any<AIApplicationPromptDataDto>(), Arg.Any<string?>())
            .Returns(new ApplicationScoringOperationInputDto { ApplicationId = request.ApplicationId!.Value });
        var applicationRepository = Substitute.For<IApplicationRepository>();
        var application = new Application
        {
            ApplicationFormId = Guid.NewGuid()
        };
        applicationRepository.GetAsync(request.ApplicationId!.Value).Returns(application);
        applicationRepository.UpdateAsync(Arg.Any<Application>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Application>()));
        var objectMapper = Substitute.For<IObjectMapper>();
        objectMapper.Map<Application, AIApplicationPromptDataDto>(Arg.Any<Application>())
            .Returns(new AIApplicationPromptDataDto
            {
                ApplicationId = request.ApplicationId!.Value,
                ApplicationFormId = application.ApplicationFormId
            });

        var localEventBus = Substitute.For<ILocalEventBus>();
        var rateLimiter = Substitute.For<IAIRateLimiter>();
        var requestedByUserId = Guid.NewGuid();

        var job = BuildJob(
            featureChecker,
            repository,
            localEventBus: localEventBus,
            rateLimiter: rateLimiter,
            applicationScoringService: scoringService,
            inputBuilder: inputBuilder,
            applicationRepository: applicationRepository,
            objectMapper: objectMapper);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = request.ApplicationId!.Value,
            RequestKey = request.RequestKey,
            RequestedByUserId = requestedByUserId,
            TenantId = request.TenantId
        });

        request.Status.ShouldBe(AIGenerationRequestStatus.Completed);
        await applicationRepository.Received(1).GetAsync(request.ApplicationId!.Value);
        await rateLimiter.Received(1).StampAsync(requestedByUserId);
        await localEventBus.Received(1).PublishAsync(
            Arg.Is<Automation.Events.ApplicationAIScoringGeneratedEvent>(x => x.ApplicationId == request.ApplicationId));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Skip_Unavailable_Stage_And_Run_Available_Stage()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);

        var repository = BuildRequestRepository(out var requests);
        var request = CreateRequest();
        requests.Add(request);
        var attachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
        attachmentRepository.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ApplicationChefsFileAttachment, bool>>>())
            .Returns(new List<ApplicationChefsFileAttachment>
            {
                CreateAttachment(request.ApplicationId!.Value)
            });

        var attachmentSummaryService = Substitute.For<IAttachmentSummaryService>();
        attachmentSummaryService.GenerateAndSaveAsync(Arg.Any<List<Guid>>(), Arg.Any<string?>())
            .Returns(new List<string>());

        var applicationAnalysisService = Substitute.For<IApplicationAnalysisService>();
        applicationAnalysisService.RegenerateAsync(Arg.Any<ApplicationAnalysisOperationInputDto>(), Arg.Any<CancellationToken>())
            .Returns("{}");
        var inputBuilder = Substitute.For<IAIApplicationInputBuilder>();
        inputBuilder.BuildApplicationAnalysisInputAsync(Arg.Any<AIApplicationPromptDataDto>(), Arg.Any<string?>())
            .Returns(new ApplicationAnalysisOperationInputDto { ApplicationId = request.ApplicationId!.Value });
        var applicationRepository = Substitute.For<IApplicationRepository>();
        var application = new Application
        {
            ApplicationFormId = Guid.NewGuid()
        };
        applicationRepository.GetAsync(request.ApplicationId!.Value).Returns(application);
        applicationRepository.UpdateAsync(Arg.Any<Application>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Application>()));
        var objectMapper = Substitute.For<IObjectMapper>();
        objectMapper.Map<Application, AIApplicationPromptDataDto>(Arg.Any<Application>())
            .Returns(new AIApplicationPromptDataDto
            {
                ApplicationId = request.ApplicationId!.Value,
                ApplicationFormId = application.ApplicationFormId
            });

        var job = BuildJob(
            featureChecker,
            repository,
            attachmentRepository: attachmentRepository,
            attachmentSummaryService: attachmentSummaryService,
            applicationAnalysisService: applicationAnalysisService,
            inputBuilder: inputBuilder,
            applicationRepository: applicationRepository,
            objectMapper: objectMapper);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = request.ApplicationId.Value,
            RequestKey = request.RequestKey,
            RequestedByUserId = Guid.NewGuid(),
            TenantId = request.TenantId
        });

        request.Status.ShouldBe(AIGenerationRequestStatus.Completed);
        await applicationRepository.Received(1).GetAsync(request.ApplicationId!.Value);
        await attachmentSummaryService.Received(1).GenerateAndSaveAsync(Arg.Any<List<Guid>>(), Arg.Any<string?>());
        await applicationAnalysisService.Received(1).RegenerateAsync(Arg.Any<ApplicationAnalysisOperationInputDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_Should_Skip_Analysis_UserFriendlyException_And_Continue_To_Scoring()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);

        var repository = BuildRequestRepository(out var requests);
        var request = CreateRequest();
        requests.Add(request);

        var analysisService = Substitute.For<IApplicationAnalysisService>();
        analysisService.RegenerateAsync(Arg.Any<ApplicationAnalysisOperationInputDto>(), Arg.Any<CancellationToken>())
            .Returns<Task>(callInfo => throw new UserFriendlyException("analysis prerequisites missing"));

        var scoringService = Substitute.For<IApplicationScoringService>();
        scoringService.RegenerateAsync(Arg.Any<ApplicationScoringOperationInputDto>(), Arg.Any<CancellationToken>())
            .Returns("{}");

        var inputBuilder = Substitute.For<IAIApplicationInputBuilder>();
        inputBuilder.BuildApplicationAnalysisInputAsync(Arg.Any<AIApplicationPromptDataDto>(), Arg.Any<string?>())
            .Returns(new ApplicationAnalysisOperationInputDto { ApplicationId = request.ApplicationId!.Value });
        inputBuilder.BuildApplicationScoringInputAsync(Arg.Any<AIApplicationPromptDataDto>(), Arg.Any<string?>())
            .Returns(new ApplicationScoringOperationInputDto { ApplicationId = request.ApplicationId!.Value });

        var applicationRepository = Substitute.For<IApplicationRepository>();
        var application = new Application
        {
            ApplicationFormId = Guid.NewGuid()
        };
        applicationRepository.GetAsync(request.ApplicationId!.Value).Returns(application);
        applicationRepository.UpdateAsync(Arg.Any<Application>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Application>()));
        var objectMapper = Substitute.For<IObjectMapper>();
        objectMapper.Map<Application, AIApplicationPromptDataDto>(Arg.Any<Application>())
            .Returns(new AIApplicationPromptDataDto
            {
                ApplicationId = request.ApplicationId!.Value,
                ApplicationFormId = application.ApplicationFormId
            });

        var localEventBus = Substitute.For<ILocalEventBus>();
        var rateLimiter = Substitute.For<IAIRateLimiter>();
        var requestedByUserId = Guid.NewGuid();

        var job = BuildJob(
            featureChecker,
            repository,
            localEventBus: localEventBus,
            rateLimiter: rateLimiter,
            applicationAnalysisService: analysisService,
            applicationScoringService: scoringService,
            inputBuilder: inputBuilder,
            applicationRepository: applicationRepository,
            objectMapper: objectMapper);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = request.ApplicationId.Value,
            RequestKey = request.RequestKey,
            RequestedByUserId = requestedByUserId,
            TenantId = request.TenantId
        });

        request.Status.ShouldBe(AIGenerationRequestStatus.Completed);
        await applicationRepository.Received(1).GetAsync(request.ApplicationId!.Value);
        await scoringService.Received(1).RegenerateAsync(Arg.Any<ApplicationScoringOperationInputDto>(), Arg.Any<CancellationToken>());
        await localEventBus.Received(1).PublishAsync(
            Arg.Is<Automation.Events.ApplicationAIScoringGeneratedEvent>(x => x.ApplicationId == request.ApplicationId));
    }

    private RunApplicationAIPipelineJob BuildJob(
        IFeatureChecker featureChecker,
        IRepository<AIGenerationRequest, Guid> generationRequestRepository,
        ILocalEventBus? localEventBus = null,
        IApplicationChefsFileAttachmentRepository? attachmentRepository = null,
        IAttachmentSummaryService? attachmentSummaryService = null,
        IApplicationAnalysisService? applicationAnalysisService = null,
        IApplicationScoringService? applicationScoringService = null,
        IAIApplicationInputBuilder? inputBuilder = null,
        IApplicationRepository? applicationRepository = null,
        IObjectMapper? objectMapper = null,
        IAIRateLimiter? rateLimiter = null)
    {
        return new RunApplicationAIPipelineJob(
            attachmentRepository ?? Substitute.For<IApplicationChefsFileAttachmentRepository>(),
            inputBuilder ?? Substitute.For<IAIApplicationInputBuilder>(),
            attachmentSummaryService ?? Substitute.For<IAttachmentSummaryService>(),
            applicationAnalysisService ?? Substitute.For<IApplicationAnalysisService>(),
            applicationScoringService ?? Substitute.For<IApplicationScoringService>(),
            applicationRepository ?? Substitute.For<IApplicationRepository>(),
            featureChecker,
            localEventBus ?? Substitute.For<ILocalEventBus>(),
            generationRequestRepository,
            Substitute.For<ICurrentTenant>(),
            objectMapper ?? Substitute.For<IObjectMapper>(),
            GetRequiredService<IUnitOfWorkManager>(),
            rateLimiter ?? Substitute.For<IAIRateLimiter>(),
            NullLogger<RunApplicationAIPipelineJob>.Instance);
    }

    private static IRepository<AIGenerationRequest, Guid> BuildRequestRepository(out List<AIGenerationRequest> requests)
    {
        var requestList = new List<AIGenerationRequest>();
        requests = requestList;
        var repository = Substitute.For<IRepository<AIGenerationRequest, Guid>>();

        repository.GetQueryableAsync()
            .Returns(Task.FromResult<IQueryable<AIGenerationRequest>>(requestList.AsQueryable()));
        repository.UpdateAsync(Arg.Any<AIGenerationRequest>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<AIGenerationRequest>()));

        return repository;
    }

    private static AIGenerationRequest CreateRequest()
    {
        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        return new AIGenerationRequest(
            Guid.NewGuid(),
            tenantId,
            Guid.NewGuid(),
            applicationId,
            AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.PipelineOperationType));
    }

    private static ApplicationChefsFileAttachment CreateAttachment(Guid applicationId)
    {
        var attachment = new ApplicationChefsFileAttachment
        {
            ApplicationId = applicationId
        };

        EntityHelper.TrySetId(attachment, () => Guid.NewGuid());

        return attachment;
    }
}
