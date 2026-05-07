using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.IO;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Extraction;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Intakes;
using Xunit;
using Xunit.Abstractions;

namespace Unity.GrantManager.AI.Operations;

public class AttachmentSummaryServiceTests : GrantManagerApplicationTestBase
{
    public AttachmentSummaryServiceTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    [Fact]
    public async Task GenerateAndSaveAsync_Uses_Streamed_Attachment_Text()
    {
        var attachmentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var stream = new MemoryStream([1, 2, 3]);
        AttachmentSummaryRequest? capturedRequest = null;

        var attachment = new ApplicationChefsFileAttachment
        {
            ApplicationId = Guid.NewGuid(),
            FileName = "test.txt",
            ChefsSubmissionId = submissionId.ToString(),
            ChefsFileId = fileId.ToString()
        };

        var attachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
        attachmentRepository.GetAsync(attachmentId).Returns(attachment);

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId, "test.txt")
            .Returns(new ChefsFileAttachmentStream(stream, "text/plain"));

        var textExtractionService = Substitute.For<ITextExtractionService>();
        textExtractionService.ExtractTextAsync("test.txt", stream, "text/plain")
            .Returns("extracted text");

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateAttachmentSummaryAsync(Arg.Do<AttachmentSummaryRequest>(request => capturedRequest = request))
            .Returns(new AttachmentSummaryResponse { Summary = "summary text" });

        var service = new AttachmentSummaryService(
            attachmentRepository,
            streamProvider,
            textExtractionService,
            aiService,
            new AIExecutionModeResolver(new ConfigurationBuilder().Build()),
            NullLogger<AttachmentSummaryService>.Instance);

        var result = await service.GenerateAndSaveAsync(attachmentId, "v1");

        result.ShouldBe("summary text");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.FileName.ShouldBe("test.txt");
        capturedRequest.ContentType.ShouldBe("text/plain");
        capturedRequest.ExtractedText.ShouldBe("extracted text");
        capturedRequest.PromptVersion.ShouldBe("v1");
        attachment.AISummary.ShouldBe("summary text");

        await attachmentRepository.Received(1).UpdateAsync(attachment);
        stream.CanRead.ShouldBeFalse();
    }
}
