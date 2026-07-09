using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Extraction;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Volo.Abp;
using Volo.Abp.Uow;
using Xunit;

namespace Unity.GrantManager.AI.Operations;

public class AttachmentSummaryServiceTests
{
    [Fact]
    public async Task GenerateAndSaveAsync_Uses_Streamed_Attachment_Text()
    {
        var attachmentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var stream = new MemoryStream([1, 2, 3]);
        AttachmentSummaryRequest? capturedRequest = null;

        var provider = CreateProvider(attachmentId, "test.txt", submissionId, fileId);

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId, "test.txt")
            .Returns(new ChefsFileAttachmentStream(stream, "text/plain"));

        var textExtractionService = Substitute.For<ITextExtractionService>();
        textExtractionService.ExtractTextAsync("test.txt", stream, "text/plain", Arg.Any<CancellationToken>())
            .Returns("extracted text");

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateAttachmentSummaryAsync(Arg.Do<AttachmentSummaryRequest>(request => capturedRequest = request), Arg.Any<CancellationToken>())
            .Returns(new AttachmentSummaryResponse { Summary = "summary text" });

        var service = CreateService(provider, streamProvider, textExtractionService, aiService);

        var result = await service.GenerateAndSaveAsync(attachmentId, "v1");

        result.ShouldBe("summary text");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.FileName.ShouldBe("test.txt");
        capturedRequest.ContentType.ShouldBe("text/plain");
        capturedRequest.ExtractedText.ShouldBe("extracted text");
        capturedRequest.PromptVersion.ShouldBe("v1");
        await provider.Received(1).UpdateAttachmentSummaryAsync(attachmentId, "summary text");
        stream.CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task GenerateAndSaveAsync_Should_Propagate_Cancellation()
    {
        var attachmentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var stream = new MemoryStream([1, 2, 3]);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        var provider = CreateProvider(attachmentId, "test.txt", submissionId, fileId);

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId, "test.txt")
            .Returns(new ChefsFileAttachmentStream(stream, "text/plain"));

        var service = CreateService(provider, streamProvider, Substitute.For<ITextExtractionService>(), Substitute.For<IAIService>());

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.GenerateAndSaveAsync(attachmentId, "v1", cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GenerateAndSaveAsync_Should_Reject_Empty_Attachment_List()
    {
        var provider = Substitute.For<IAttachmentSummaryDataProvider>();
        var service = CreateService(provider, Substitute.For<IChefsFileAttachmentStreamProvider>(), Substitute.For<ITextExtractionService>(), Substitute.For<IAIService>());

        await Should.ThrowAsync<UserFriendlyException>(() => service.GenerateAndSaveAsync([], "v1"));
    }

    [Fact]
    public async Task GenerateAndSaveAsync_Should_Use_Batch_Mode_When_Configured()
    {
        var firstAttachmentId = Guid.NewGuid();
        var secondAttachmentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var fileId1 = Guid.NewGuid();
        var fileId2 = Guid.NewGuid();
        var stream1 = new MemoryStream([1, 2, 3]);
        var stream2 = new MemoryStream([4, 5, 6]);
        var applicationId = Guid.NewGuid();

        var provider = Substitute.For<IAttachmentSummaryDataProvider>();
        provider.GetAttachmentAsync(firstAttachmentId).Returns(new AttachmentSummarySource(firstAttachmentId, "first.txt", submissionId.ToString(), fileId1.ToString()));
        provider.GetAttachmentAsync(secondAttachmentId).Returns(new AttachmentSummarySource(secondAttachmentId, "second.txt", submissionId.ToString(), fileId2.ToString()));
        provider.GetApplicationAttachmentIdsAsync(applicationId).Returns(new List<Guid> { firstAttachmentId, secondAttachmentId });
        provider.GetApplicationAttachmentIdsAsync(Arg.Is<Guid>(id => id != applicationId)).Returns(new List<Guid>());

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId1, "first.txt")
            .Returns(new ChefsFileAttachmentStream(stream1, "text/plain"));
        streamProvider.OpenAsync(submissionId, fileId2, "second.txt")
            .Returns(new ChefsFileAttachmentStream(stream2, "text/plain"));

        var textExtractionService = Substitute.For<ITextExtractionService>();
        textExtractionService.ExtractTextAsync("first.txt", stream1, "text/plain", Arg.Any<CancellationToken>())
            .Returns("first extracted");
        textExtractionService.ExtractTextAsync("second.txt", stream2, "text/plain", Arg.Any<CancellationToken>())
            .Returns("second extracted");

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateAttachmentSummaryBatchAsync(Arg.Any<AttachmentSummaryBatchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AttachmentSummaryBatchResponse
            {
                Attachments =
                {
                    new AttachmentSummaryBatchItemResponse { AttachmentId = firstAttachmentId.ToString(), Summary = "first summary" },
                    new AttachmentSummaryBatchItemResponse { AttachmentId = secondAttachmentId.ToString(), Summary = "second summary" }
                }
            });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Azure:Operations:{AIExecutionModeResolver.AttachmentSummaryOperation}:ExecutionMode"] = "Batch"
            })
            .Build();

        var service = new AttachmentSummaryService(
            provider,
            streamProvider,
            textExtractionService,
            aiService,
            Substitute.For<IAIGenerationPrerequisiteValidator>(),
            new AIExecutionModeResolver(configuration),
            CreateUnitOfWorkManager(),
            NullLogger<AttachmentSummaryService>.Instance,
            Substitute.For<IStringLocalizer<AIResource>>());

        var summaries = await service.GenerateAndSaveAsync([firstAttachmentId, secondAttachmentId], "v1");

        summaries.ShouldBe(["first summary", "second summary"]);
        await aiService.Received(1).GenerateAttachmentSummaryBatchAsync(Arg.Any<AttachmentSummaryBatchRequest>(), Arg.Any<CancellationToken>());
        await provider.Received(1).UpdateAttachmentSummaryAsync(firstAttachmentId, "first summary");
        await provider.Received(1).UpdateAttachmentSummaryAsync(secondAttachmentId, "second summary");
    }

    private static AttachmentSummaryService CreateService(
        IAttachmentSummaryDataProvider provider,
        IChefsFileAttachmentStreamProvider streamProvider,
        ITextExtractionService textExtractionService,
        IAIService aiService)
    {
        return new AttachmentSummaryService(
            provider,
            streamProvider,
            textExtractionService,
            aiService,
            Substitute.For<IAIGenerationPrerequisiteValidator>(),
            new AIExecutionModeResolver(new ConfigurationBuilder().Build()),
            CreateUnitOfWorkManager(),
            NullLogger<AttachmentSummaryService>.Instance,
            Substitute.For<IStringLocalizer<AIResource>>());
    }

    private static IAttachmentSummaryDataProvider CreateProvider(
        Guid attachmentId,
        string fileName,
        Guid submissionId,
        Guid fileId,
        Action<string>? savedSummary = null)
    {
        var provider = Substitute.For<IAttachmentSummaryDataProvider>();
        provider.GetAttachmentAsync(attachmentId).Returns(new AttachmentSummarySource(attachmentId, fileName, submissionId.ToString(), fileId.ToString()));
        provider.GetApplicationAttachmentIdsAsync(Arg.Any<Guid>()).Returns(new List<Guid> { attachmentId });
        provider.UpdateAttachmentSummaryAsync(attachmentId, Arg.Any<string>()).Returns(callInfo =>
        {
            savedSummary?.Invoke(callInfo.ArgAt<string>(1));
            return Task.CompletedTask;
        });
        return provider;
    }

    private static IUnitOfWorkManager CreateUnitOfWorkManager()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Begin(Arg.Any<AbpUnitOfWorkOptions>(), Arg.Any<bool>())
            .Returns(unitOfWork);
        return unitOfWorkManager;
    }
}
