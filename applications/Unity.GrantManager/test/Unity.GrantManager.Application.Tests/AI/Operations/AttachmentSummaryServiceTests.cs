using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NPOI.XWPF.UserModel;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Extraction;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
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
        string? savedSummary = null;

        var persistence = CreatePersistence(attachmentId, "test.txt", submissionId, fileId, savedSummary: summary => savedSummary = summary);

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId, "test.txt")
            .Returns(new ChefsFileAttachmentStream(stream, "text/plain"));

        var textExtractionService = Substitute.For<ITextExtractionService>();
        textExtractionService.ExtractTextAsync("test.txt", stream, "text/plain")
            .Returns("extracted text");

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateAttachmentSummaryAsync(Arg.Do<AttachmentSummaryRequest>(request => capturedRequest = request))
            .Returns(new AttachmentSummaryResponse { Summary = "summary text" });

        var service = CreateService(
            persistence,
            streamProvider,
            textExtractionService,
            aiService);

        var result = await service.GenerateAndSaveAsync(attachmentId, "v1");

        result.ShouldBe("summary text");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.FileName.ShouldBe("test.txt");
        capturedRequest.ContentType.ShouldBe("text/plain");
        capturedRequest.ExtractedText.ShouldBe("extracted text");
        capturedRequest.PromptVersion.ShouldBe("v1");
        savedSummary.ShouldBe("summary text");
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

        var persistence = CreatePersistence(attachmentId, "test.txt", submissionId, fileId);

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId, "test.txt")
            .Returns(new ChefsFileAttachmentStream(stream, "text/plain"));

        var service = CreateService(
            persistence,
            streamProvider,
            Substitute.For<ITextExtractionService>(),
            Substitute.For<IAIService>());

        await Should.ThrowAsync<OperationCanceledException>(() =>
            service.GenerateAndSaveAsync(attachmentId, "v1", cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GenerateAndSaveAsync_Should_Reject_Empty_Attachment_List()
    {
        var persistence = Substitute.For<IAttachmentSummaryPersistence>();
        var service = CreateService(
            persistence,
            Substitute.For<IChefsFileAttachmentStreamProvider>(),
            Substitute.For<ITextExtractionService>(),
            Substitute.For<IAIService>());

        await Should.ThrowAsync<UserFriendlyException>(() => service.GenerateAndSaveAsync([], "v1"));

        await persistence.DidNotReceive().LoadApplicationAttachmentIdsAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GenerateAndSaveAsync_Should_Not_Call_AI_When_Supported_File_Extraction_Is_Empty()
    {
        var attachmentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var stream = new MemoryStream([1, 2, 3]);
        string? savedSummary = null;

        var persistence = CreatePersistence(attachmentId, "test.docx", submissionId, fileId, summary => savedSummary = summary);

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId, "test.docx")
            .Returns(new ChefsFileAttachmentStream(stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));

        var textExtractionService = Substitute.For<ITextExtractionService>();
        textExtractionService.ExtractTextAsync("test.docx", stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            .Returns(string.Empty);

        var aiService = Substitute.For<IAIService>();
        var service = CreateService(
            persistence,
            streamProvider,
            textExtractionService,
            aiService);

        var result = await service.GenerateAndSaveAsync(attachmentId, "v1");

        result.ShouldBe("Attachment text could not be extracted for AI summary generation.");
        savedSummary.ShouldBe(result);
        await aiService.DidNotReceive().GenerateAttachmentSummaryAsync(Arg.Any<AttachmentSummaryRequest>());
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

        var firstAttachment = new ApplicationChefsFileAttachment
        {
            ApplicationId = Guid.NewGuid(),
            FileName = "first.txt",
            ChefsSubmissionId = submissionId.ToString(),
            ChefsFileId = fileId1.ToString()
        };

        var secondAttachment = new ApplicationChefsFileAttachment
        {
            ApplicationId = firstAttachment.ApplicationId,
            FileName = "second.txt",
            ChefsSubmissionId = submissionId.ToString(),
            ChefsFileId = fileId2.ToString()
        };

        var attachmentRepository = Substitute.For<IApplicationChefsFileAttachmentRepository>();
        attachmentRepository.GetAsync(firstAttachmentId).Returns(firstAttachment);
        attachmentRepository.GetAsync(secondAttachmentId).Returns(secondAttachment);

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
        aiService.GenerateAttachmentSummaryBatchAsync(Arg.Any<AttachmentSummaryBatchRequest>())
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
            attachmentRepository,
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
        await aiService.Received(1).GenerateAttachmentSummaryBatchAsync(Arg.Any<AttachmentSummaryBatchRequest>());
        await attachmentRepository.Received(1).UpdateAsync(firstAttachment);
        await attachmentRepository.Received(1).UpdateAsync(secondAttachment);
    }

    [Fact]
    public async Task GenerateAndSaveAsync_Should_Pass_Extracted_Docx_Text_To_AI()
    {
        var attachmentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var stream = CreateDocxStream("Riverside mock attachment content");
        AttachmentSummaryRequest? capturedRequest = null;

        var persistence = CreatePersistence(attachmentId, "riverside-profile.docx", submissionId, fileId);

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId, "riverside-profile.docx")
            .Returns(new ChefsFileAttachmentStream(stream, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"));

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateAttachmentSummaryAsync(Arg.Do<AttachmentSummaryRequest>(request => capturedRequest = request))
            .Returns(new AttachmentSummaryResponse { Summary = "summary text" });

        var service = CreateService(
            persistence,
            streamProvider,
            new TextExtractionService(NullLogger<TextExtractionService>.Instance),
            aiService);

        var result = await service.GenerateAndSaveAsync(attachmentId, "v1");

        result.ShouldBe("summary text");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.ExtractedText.ShouldNotBeNull();
        capturedRequest.ExtractedText.ShouldContain("Riverside mock attachment content");
        await aiService.Received(1).GenerateAttachmentSummaryAsync(Arg.Any<AttachmentSummaryRequest>());
    }

    [Fact]
    public async Task GenerateAndSaveAsync_Should_Pass_Extracted_Text_From_Text_Attachment_To_AI()
    {
        var attachmentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var attachmentText = "Mock CHEFS attachment content for extraction.";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(attachmentText));
        AttachmentSummaryRequest? capturedRequest = null;

        var persistence = CreatePersistence(attachmentId, "mock-attachment.txt", submissionId, fileId);

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId, "mock-attachment.txt")
            .Returns(new ChefsFileAttachmentStream(stream, "text/plain"));

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateAttachmentSummaryAsync(Arg.Do<AttachmentSummaryRequest>(request => capturedRequest = request))
            .Returns(new AttachmentSummaryResponse { Summary = "summary text" });

        var service = CreateService(
            persistence,
            streamProvider,
            new TextExtractionService(NullLogger<TextExtractionService>.Instance),
            aiService);

        var result = await service.GenerateAndSaveAsync(attachmentId, "v1");

        result.ShouldBe("summary text");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.ExtractedText.ShouldBe(attachmentText);
        await aiService.Received(1).GenerateAttachmentSummaryAsync(Arg.Any<AttachmentSummaryRequest>());
    }

    [Fact]
    public async Task GenerateAndSaveAsync_Should_Pass_Extracted_Pdf_Text_To_AI()
    {
        var attachmentId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var attachmentText = "Mock CHEFS PDF attachment content";
        var stream = CreatePdfStream(attachmentText);
        AttachmentSummaryRequest? capturedRequest = null;

        var persistence = CreatePersistence(attachmentId, "mock-attachment.pdf", submissionId, fileId);

        var streamProvider = Substitute.For<IChefsFileAttachmentStreamProvider>();
        streamProvider.OpenAsync(submissionId, fileId, "mock-attachment.pdf")
            .Returns(new ChefsFileAttachmentStream(stream, "application/pdf"));

        var aiService = Substitute.For<IAIService>();
        aiService.GenerateAttachmentSummaryAsync(Arg.Do<AttachmentSummaryRequest>(request => capturedRequest = request))
            .Returns(new AttachmentSummaryResponse { Summary = "summary text" });

        var service = CreateService(
            persistence,
            streamProvider,
            new TextExtractionService(NullLogger<TextExtractionService>.Instance),
            aiService);

        var result = await service.GenerateAndSaveAsync(attachmentId, "v1");

        result.ShouldBe("summary text");
        capturedRequest.ShouldNotBeNull();
        capturedRequest.ExtractedText.ShouldContain(attachmentText);
        await aiService.Received(1).GenerateAttachmentSummaryAsync(Arg.Any<AttachmentSummaryRequest>());
    }

    private static AttachmentSummaryService CreateService(
        IAttachmentSummaryPersistence persistence,
        IChefsFileAttachmentStreamProvider streamProvider,
        ITextExtractionService textExtractionService,
        IAIService aiService)
    {
        return new AttachmentSummaryService(
            persistence,
            streamProvider,
            textExtractionService,
            aiService,
            Substitute.For<IAIGenerationPrerequisiteValidator>(),
            new AIExecutionModeResolver(new ConfigurationBuilder().Build()),
            CreateUnitOfWorkManager(),
            NullLogger<AttachmentSummaryService>.Instance,
            Substitute.For<IStringLocalizer<AIResource>>());
    }

    private static IAttachmentSummaryPersistence CreatePersistence(
        Guid attachmentId,
        string fileName,
        Guid submissionId,
        Guid fileId,
        Action<string>? savedSummary = null)
    {
        var persistence = Substitute.For<IAttachmentSummaryPersistence>();
        persistence.LoadAsync(attachmentId).Returns(new AttachmentSummarySource(
            attachmentId,
            fileName,
            submissionId.ToString(),
            fileId.ToString()));
        persistence.LoadApplicationAttachmentIdsAsync(Arg.Any<Guid>()).Returns(new List<Guid>());
        persistence.SaveSummaryAsync(attachmentId, Arg.Any<string>()).Returns(callInfo =>
        {
            savedSummary?.Invoke(callInfo.ArgAt<string>(1));
            return Task.CompletedTask;
        });
        return persistence;
    }

    private static MemoryStream CreateDocxStream(string paragraphText)
    {
        var writeStream = new MemoryStream();
        using (var document = new XWPFDocument())
        {
            document.CreateParagraph().CreateRun().SetText(paragraphText);
            document.Write(writeStream);
        }

        return new MemoryStream(writeStream.ToArray());
    }

    private static MemoryStream CreatePdfStream(string text)
    {
        static string EscapePdfText(string value) =>
            value.Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)");

        var contentStream = $"BT /F1 18 Tf 72 144 Td ({EscapePdfText(text)}) Tj ET\n";
        var contentBytes = Encoding.ASCII.GetBytes(contentStream);

        var objects = new[]
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n",
            $"4 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{contentStream}endstream\nendobj\n",
            "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n"
        };

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");
        var offsets = new List<int> { 0 };

        foreach (var pdfObject in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(pdfObject);
        }

        var xrefStart = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append("xref\n");
        builder.Append("0 6\n");
        builder.Append("0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
        {
            builder.Append($"{offset:0000000000} 00000 n \n");
        }

        builder.Append("trailer\n");
        builder.Append("<< /Size 6 /Root 1 0 R >>\n");
        builder.Append("startxref\n");
        builder.Append($"{xrefStart}\n");
        builder.Append("%%EOF\n");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
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
