using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Unity.Notifications.EmailNotifications;
using Volo.Abp.Users;
using Xunit;

namespace Unity.Notifications.Emails;

public class EmailAttachmentServiceTests
{
    [Fact]
    public async Task CopyTemplateAttachmentsAsync_CreatesAnEmailOwnedS3Copy()
    {
        var templateId = Guid.NewGuid();
        var emailLogId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var source = CreateAttachment("template/source.pdf", "source.pdf", templateId: templateId);
        var repository = Substitute.For<IEmailLogAttachmentRepository>();
        var s3 = Substitute.For<IAmazonS3>();
        var service = CreateService(repository, s3);
        CopyObjectRequest? copyRequest = null;
        List<EmailLogAttachment>? insertedAttachments = null;

        repository.GetByTemplateIdAsync(templateId)
            .Returns(Task.FromResult(new List<EmailLogAttachment> { source }));
        repository.GetByEmailLogIdAsync(emailLogId)
            .Returns(Task.FromResult(new List<EmailLogAttachment>()));
        s3.GetObjectMetadataAsync(
                Arg.Any<GetObjectMetadataRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetObjectMetadataResponse()));
        s3.CopyObjectAsync(
                Arg.Do<CopyObjectRequest>(request => copyRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CopyObjectResponse()));
        repository.InsertManyAsync(
                Arg.Do<IEnumerable<EmailLogAttachment>>(attachments => insertedAttachments = attachments.ToList()),
                true,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var copiedCount = await service.CopyTemplateAttachmentsAsync(templateId, emailLogId, tenantId);

        copiedCount.ShouldBe(1);
        copyRequest.ShouldNotBeNull();
        copyRequest.SourceKey.ShouldBe(source.S3ObjectKey);
        copyRequest.DestinationKey.ShouldNotBe(source.S3ObjectKey);
        copyRequest.DestinationKey.ShouldStartWith($"Email/Attachments/{tenantId}/{emailLogId}/");
        insertedAttachments.ShouldNotBeNull();
        insertedAttachments.Count.ShouldBe(1);
        insertedAttachments[0].S3ObjectKey.ShouldBe(copyRequest.DestinationKey);
        insertedAttachments[0].EmailLogId.ShouldBe(emailLogId);
        insertedAttachments[0].OriginTemplateId.ShouldBe(templateId);
        insertedAttachments[0].TemplateId.ShouldBeNull();
    }

    [Fact]
    public async Task ReplaceTemplateAttachmentsAsync_WhenSourceIsMissing_PreservesExistingAttachments()
    {
        var templateId = Guid.NewGuid();
        var emailLogId = Guid.NewGuid();
        var source = CreateAttachment("template/missing.pdf", "missing.pdf", templateId: templateId);
        var previous = CreateAttachment("email/previous.pdf", "previous.pdf", emailLogId, Guid.NewGuid());
        var repository = Substitute.For<IEmailLogAttachmentRepository>();
        var s3 = Substitute.For<IAmazonS3>();
        var service = CreateService(repository, s3);

        repository.GetByTemplateIdAsync(templateId)
            .Returns(Task.FromResult(new List<EmailLogAttachment> { source }));
        repository.GetOriginAttachmentsByEmailLogIdAsync(emailLogId)
            .Returns(Task.FromResult(new List<EmailLogAttachment> { previous }));
        s3.GetObjectMetadataAsync(
                Arg.Any<GetObjectMetadataRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetObjectMetadataResponse>(CreateMissingObjectException()));

        var exception = await Should.ThrowAsync<MissingEmailAttachmentsException>(() =>
            service.ReplaceTemplateAttachmentsAsync(templateId, emailLogId, tenantId: null));

        exception.Message.ShouldBe(
            "This template contains attachments that cannot be found: missing.pdf. Re-upload them in the email template before using it.");
        await repository.DidNotReceive().DeleteAsync(
            Arg.Any<EmailLogAttachment>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        await repository.DidNotReceive().InsertManyAsync(
            Arg.Any<IEnumerable<EmailLogAttachment>>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        await s3.DidNotReceive().CopyObjectAsync(
            Arg.Any<CopyObjectRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceTemplateAttachmentsAsync_ReplacesOnlyTemplateOriginAttachments()
    {
        var newTemplateId = Guid.NewGuid();
        var oldTemplateId = Guid.NewGuid();
        var emailLogId = Guid.NewGuid();
        var source = CreateAttachment("template/new.pdf", "new.pdf", templateId: newTemplateId);
        var previous = CreateAttachment("email/old.pdf", "old.pdf", emailLogId, oldTemplateId);
        var manual = CreateAttachment("email/manual.pdf", "manual.pdf", emailLogId);
        var repository = Substitute.For<IEmailLogAttachmentRepository>();
        var s3 = Substitute.For<IAmazonS3>();
        var service = CreateService(repository, s3);
        DeleteObjectRequest? deleteRequest = null;

        repository.GetByTemplateIdAsync(newTemplateId)
            .Returns(Task.FromResult(new List<EmailLogAttachment> { source }));
        repository.GetOriginAttachmentsByEmailLogIdAsync(emailLogId)
            .Returns(Task.FromResult(new List<EmailLogAttachment> { previous }));
        s3.GetObjectMetadataAsync(
                Arg.Any<GetObjectMetadataRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GetObjectMetadataResponse()));
        s3.CopyObjectAsync(
                Arg.Any<CopyObjectRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new CopyObjectResponse()));
        s3.DeleteObjectAsync(
                Arg.Do<DeleteObjectRequest>(request => deleteRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeleteObjectResponse()));
        repository.InsertManyAsync(
                Arg.Any<IEnumerable<EmailLogAttachment>>(),
                true,
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        repository.HasOtherReferencesAsync(previous.S3ObjectKey, previous.Id)
            .Returns(Task.FromResult(false));
        repository.DeleteAsync(previous, true, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var copiedCount = await service.ReplaceTemplateAttachmentsAsync(
            newTemplateId,
            emailLogId,
            tenantId: null);

        copiedCount.ShouldBe(1);
        await repository.Received(1).DeleteAsync(
            Arg.Is<EmailLogAttachment>(attachment => ReferenceEquals(attachment, previous)),
            true,
            Arg.Any<CancellationToken>());
        await repository.DidNotReceive().DeleteAsync(
            Arg.Is<EmailLogAttachment>(attachment => ReferenceEquals(attachment, manual)),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        deleteRequest.ShouldNotBeNull();
        deleteRequest.Key.ShouldBe(previous.S3ObjectKey);
    }

    [Fact]
    public async Task DeleteAttachmentAsync_WhenKeyIsShared_PreservesTheS3Object()
    {
        var attachment = CreateAttachment("legacy/shared.pdf", "shared.pdf", Guid.NewGuid(), Guid.NewGuid());
        var repository = Substitute.For<IEmailLogAttachmentRepository>();
        var s3 = Substitute.For<IAmazonS3>();
        var service = CreateService(repository, s3);

        repository.HasOtherReferencesAsync(attachment.S3ObjectKey, attachment.Id)
            .Returns(Task.FromResult(true));
        repository.DeleteAsync(attachment, true, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await service.DeleteAttachmentAsync(attachment);

        await repository.Received(1).DeleteAsync(
            attachment,
            true,
            Arg.Any<CancellationToken>());
        await s3.DidNotReceive().DeleteObjectAsync(
            Arg.Any<DeleteObjectRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateEmailAttachmentsAsync_ReportsEveryMissingFile()
    {
        var emailLogId = Guid.NewGuid();
        var first = CreateAttachment("email/first.pdf", "first.pdf", emailLogId);
        var second = CreateAttachment("email/second.pdf", "second.pdf", emailLogId);
        var repository = Substitute.For<IEmailLogAttachmentRepository>();
        var s3 = Substitute.For<IAmazonS3>();
        var service = CreateService(repository, s3);

        repository.GetByEmailLogIdAsync(emailLogId)
            .Returns(Task.FromResult(new List<EmailLogAttachment> { second, first }));
        s3.GetObjectMetadataAsync(
                Arg.Any<GetObjectMetadataRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetObjectMetadataResponse>(CreateMissingObjectException()));

        var exception = await Should.ThrowAsync<MissingEmailAttachmentsException>(() =>
            service.ValidateEmailAttachmentsAsync(emailLogId));

        exception.FileNames.ShouldBe(new[] { "first.pdf", "second.pdf" });
        exception.Message.ShouldBe(
            "One or more email attachments cannot be found: first.pdf, second.pdf. Remove and re-upload them before sending.");
    }

    [Fact]
    public async Task ValidateTemplateAttachmentsAsync_ReportsEveryMissingFile()
    {
        var templateId = Guid.NewGuid();
        var first = CreateAttachment("template/first.pdf", "first.pdf", templateId: templateId);
        var second = CreateAttachment("template/second.pdf", "second.pdf", templateId: templateId);
        var repository = Substitute.For<IEmailLogAttachmentRepository>();
        var s3 = Substitute.For<IAmazonS3>();
        var service = CreateService(repository, s3);

        repository.GetByTemplateIdAsync(templateId)
            .Returns(Task.FromResult(new List<EmailLogAttachment> { second, first }));
        s3.GetObjectMetadataAsync(
                Arg.Any<GetObjectMetadataRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetObjectMetadataResponse>(CreateMissingObjectException()));

        var exception = await Should.ThrowAsync<MissingEmailAttachmentsException>(() =>
            service.ValidateTemplateAttachmentsAsync(templateId));

        exception.FileNames.ShouldBe(new[] { "first.pdf", "second.pdf" });
        exception.Message.ShouldBe(
            "This template contains attachments that cannot be found: first.pdf, second.pdf. Re-upload them in the email template before using it.");
    }

    private static EmailAttachmentService CreateService(
        IEmailLogAttachmentRepository repository,
        IAmazonS3 s3)
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["S3:Bucket"].Returns("test-bucket");

        return new EmailAttachmentService(
            configuration,
            repository,
            Substitute.For<ICurrentUser>(),
            NullLogger<EmailAttachmentService>.Instance,
            s3);
    }

    private static EmailLogAttachment CreateAttachment(
        string s3ObjectKey,
        string fileName,
        Guid? emailLogId = null,
        Guid? templateId = null)
    {
        return new EmailLogAttachment
        {
            EmailLogId = emailLogId,
            TemplateId = emailLogId.HasValue ? null : templateId,
            OriginTemplateId = emailLogId.HasValue ? templateId : null,
            S3ObjectKey = s3ObjectKey,
            FileName = fileName,
            DisplayName = fileName,
            ContentType = "application/pdf",
            FileSize = 123,
            Time = DateTime.UtcNow,
            UserId = Guid.NewGuid()
        };
    }

    private static AmazonS3Exception CreateMissingObjectException()
    {
        return new AmazonS3Exception(
            "The specified key does not exist.",
            ErrorType.Sender,
            "NoSuchKey",
            "request-id",
            HttpStatusCode.NotFound);
    }
}
