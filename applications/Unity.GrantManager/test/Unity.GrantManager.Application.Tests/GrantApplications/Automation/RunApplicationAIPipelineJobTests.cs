using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
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
    private static readonly Guid AttachmentSummaryOperationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ApplicationAnalysisOperationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ApplicationScoringOperationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PipelineOperationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid DefaultOperationId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    [Fact]
    public async Task ExecuteAsync_Should_Mark_Request_Completed_When_Features_Disabled()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync(Arg.Any<string>()).Returns(false);

        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var job = BuildJob(featureChecker);
        var requestedByUserId = Guid.NewGuid();

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = applicationId,
            RequestedByUserId = requestedByUserId,
            TenantId = tenantId
        });

    }

    [Fact]
    public async Task ExecuteAsync_Should_Publish_Event_When_Pipeline_Scoring_Completes()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(false);
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(true);

        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var scoringService = Substitute.For<IApplicationScoringService>();
        scoringService.RegenerateAsync(Arg.Any<ApplicationScoringOperationInputDto>(), Arg.Any<CancellationToken>())
            .Returns("{}");
        var inputBuilder = Substitute.For<IAIApplicationInputBuilder>();
        inputBuilder.BuildApplicationScoringInputAsync(Arg.Any<AIApplicationPromptDataDto>(), Arg.Any<string?>())
            .Returns(new ApplicationScoringOperationInputDto { ApplicationId = applicationId });
        var applicationRepository = Substitute.For<IApplicationRepository>();
        var application = new Application
        {
            ApplicationFormId = Guid.NewGuid()
        };
        applicationRepository.GetAsync(applicationId).Returns(application);
        applicationRepository.UpdateAsync(Arg.Any<Application>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Application>()));
        var objectMapper = Substitute.For<IObjectMapper>();
        objectMapper.Map<Application, AIApplicationPromptDataDto>(Arg.Any<Application>())
            .Returns(new AIApplicationPromptDataDto
            {
                ApplicationId = applicationId,
                ApplicationFormId = application.ApplicationFormId
            });

        var localEventBus = Substitute.For<ILocalEventBus>();
        var requestedByUserId = Guid.NewGuid();

        var job = BuildJob(
            featureChecker,
            localEventBus: localEventBus,
            applicationScoringService: scoringService,
            inputBuilder: inputBuilder,
            applicationRepository: applicationRepository,
            objectMapper: objectMapper);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = applicationId,
            RequestedByUserId = requestedByUserId,
            TenantId = tenantId
        });

        await applicationRepository.Received(1).GetAsync(applicationId);
        await localEventBus.Received(1).PublishAsync(
            Arg.Is<Automation.Events.ApplicationAIScoringGeneratedEvent>(x => x.ApplicationId == applicationId));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Skip_Unavailable_Stage_And_Run_Available_Stage()
    {
        var featureChecker = Substitute.For<IFeatureChecker>();
        featureChecker.IsEnabledAsync("Unity.AI.AttachmentSummaries").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.ApplicationAnalysis").Returns(true);
        featureChecker.IsEnabledAsync("Unity.AI.Scoring").Returns(false);

        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var attachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
        attachmentRepository.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<ApplicationChefsFileAttachment, bool>>>())
            .Returns(new List<ApplicationChefsFileAttachment>
            {
                CreateAttachment(applicationId)
            });

        var attachmentSummaryService = Substitute.For<IAttachmentSummaryService>();
        attachmentSummaryService.GenerateAndSaveAsync(Arg.Any<List<Guid>>(), Arg.Any<string?>())
            .Returns(new List<string>());

        var applicationAnalysisService = Substitute.For<IApplicationAnalysisService>();
        applicationAnalysisService.RegenerateAsync(Arg.Any<ApplicationAnalysisOperationInputDto>(), Arg.Any<CancellationToken>())
            .Returns("{}");
        var inputBuilder = Substitute.For<IAIApplicationInputBuilder>();
        inputBuilder.BuildApplicationAnalysisInputAsync(Arg.Any<AIApplicationPromptDataDto>(), Arg.Any<string?>())
            .Returns(new ApplicationAnalysisOperationInputDto { ApplicationId = applicationId });
        var applicationRepository = Substitute.For<IApplicationRepository>();
        var application = new Application
        {
            ApplicationFormId = Guid.NewGuid()
        };
        applicationRepository.GetAsync(applicationId).Returns(application);
        applicationRepository.UpdateAsync(Arg.Any<Application>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Application>()));
        var objectMapper = Substitute.For<IObjectMapper>();
        objectMapper.Map<Application, AIApplicationPromptDataDto>(Arg.Any<Application>())
            .Returns(new AIApplicationPromptDataDto
            {
                ApplicationId = applicationId,
                ApplicationFormId = application.ApplicationFormId
            });

        var job = BuildJob(
            featureChecker,
            attachmentRepository: attachmentRepository,
            attachmentSummaryService: attachmentSummaryService,
            applicationAnalysisService: applicationAnalysisService,
            inputBuilder: inputBuilder,
            applicationRepository: applicationRepository,
            objectMapper: objectMapper);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = applicationId,
            RequestedByUserId = Guid.NewGuid(),
            TenantId = tenantId
        });

        await applicationRepository.Received(1).GetAsync(applicationId);
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

        var applicationId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var analysisService = Substitute.For<IApplicationAnalysisService>();
        analysisService.RegenerateAsync(Arg.Any<ApplicationAnalysisOperationInputDto>(), Arg.Any<CancellationToken>())
            .Returns<Task>(callInfo => throw new UserFriendlyException("analysis prerequisites missing"));

        var scoringService = Substitute.For<IApplicationScoringService>();
        scoringService.RegenerateAsync(Arg.Any<ApplicationScoringOperationInputDto>(), Arg.Any<CancellationToken>())
            .Returns("{}");

        var inputBuilder = Substitute.For<IAIApplicationInputBuilder>();
        inputBuilder.BuildApplicationAnalysisInputAsync(Arg.Any<AIApplicationPromptDataDto>(), Arg.Any<string?>())
            .Returns(new ApplicationAnalysisOperationInputDto { ApplicationId = applicationId });
        inputBuilder.BuildApplicationScoringInputAsync(Arg.Any<AIApplicationPromptDataDto>(), Arg.Any<string?>())
            .Returns(new ApplicationScoringOperationInputDto { ApplicationId = applicationId });

        var applicationRepository = Substitute.For<IApplicationRepository>();
        var application = new Application
        {
            ApplicationFormId = Guid.NewGuid()
        };
        applicationRepository.GetAsync(applicationId).Returns(application);
        applicationRepository.UpdateAsync(Arg.Any<Application>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Application>()));
        var objectMapper = Substitute.For<IObjectMapper>();
        objectMapper.Map<Application, AIApplicationPromptDataDto>(Arg.Any<Application>())
            .Returns(new AIApplicationPromptDataDto
            {
                ApplicationId = applicationId,
                ApplicationFormId = application.ApplicationFormId
            });

        var localEventBus = Substitute.For<ILocalEventBus>();
        var requestedByUserId = Guid.NewGuid();

        var job = BuildJob(
            featureChecker,
            localEventBus: localEventBus,
            applicationAnalysisService: analysisService,
            applicationScoringService: scoringService,
            inputBuilder: inputBuilder,
            applicationRepository: applicationRepository,
            objectMapper: objectMapper);

        await job.ExecuteAsync(new RunApplicationAIPipelineJobArgs
        {
            ApplicationId = applicationId,
            RequestedByUserId = requestedByUserId,
            TenantId = tenantId
        });

        await applicationRepository.Received(1).GetAsync(applicationId);
        await scoringService.Received(1).RegenerateAsync(Arg.Any<ApplicationScoringOperationInputDto>(), Arg.Any<CancellationToken>());
        await localEventBus.Received(1).PublishAsync(
            Arg.Is<Automation.Events.ApplicationAIScoringGeneratedEvent>(x => x.ApplicationId == applicationId));
    }

    private RunApplicationAIPipelineJob BuildJob(
        IFeatureChecker featureChecker,
        ILocalEventBus? localEventBus = null,
        IApplicationChefsFileAttachmentRepository? attachmentRepository = null,
        IAttachmentSummaryService? attachmentSummaryService = null,
        IApplicationAnalysisService? applicationAnalysisService = null,
        IApplicationScoringService? applicationScoringService = null,
        IAIApplicationInputBuilder? inputBuilder = null,
        IApplicationRepository? applicationRepository = null,
        IObjectMapper? objectMapper = null)
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
            Substitute.For<ICurrentTenant>(),
            objectMapper ?? Substitute.For<IObjectMapper>(),
            NullLogger<RunApplicationAIPipelineJob>.Instance);
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
