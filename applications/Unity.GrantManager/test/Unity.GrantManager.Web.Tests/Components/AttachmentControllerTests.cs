using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Controllers;
using Unity.GrantManager.Intakes;
using Unity.Notifications.Emails;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Validation;
using Xunit;

namespace Unity.GrantManager.Components
{
    [Collection(WebTestCollection.Name)]
    public class AttachmentControllerTests
    {
        [Fact]
        public async Task UploadApplicationAttachments_InvalidInput_ReturnsBadRequest()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var invalidFile = new FormFile(
                baseStream: new System.IO.MemoryStream(Array.Empty<byte>()),
                baseStreamOffset: 0,
                length: 0,
                name: "invalidFile",
                fileName: "invalidFile.exe"
            );

            var files = new List<IFormFile> { invalidFile };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            var badRequestResult = result.ValidationErrors[0].ErrorMessage;
            Assert.Contains("Invalid file type", badRequestResult);
        }

        [Fact]
        public async Task UploadApplicationAttachments_ExtensionNotOnOldDenylist_ReturnsBadRequest()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            // .ps1 was never on the old denylist (exe/sh/ksh/bat/cmd) so it would have been
            // accepted before this fix; the allowlist must reject it since it's not a permitted type.
            var scriptFile = new FormFile(
                baseStream: new System.IO.MemoryStream(Array.Empty<byte>()),
                baseStreamOffset: 0,
                length: 0,
                name: "scriptFile",
                fileName: "malicious.ps1"
            );

            var files = new List<IFormFile> { scriptFile };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            var badRequestResult = result.ValidationErrors[0].ErrorMessage;
            Assert.Contains("Invalid file type", badRequestResult);
            await fileAppService.DidNotReceive().SaveBlobAsync(Arg.Any<SaveBlobInputDto>());
        }

        [Fact]
        public async Task UploadApplicationAttachments_ContentDoesNotMatchPdfSignature_ReturnsBadRequest()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var fakePdfBytes = System.Text.Encoding.UTF8.GetBytes("this is not really a pdf, just renamed text");
            var fakePdfFile = new FormFile(
                baseStream: new System.IO.MemoryStream(fakePdfBytes),
                baseStreamOffset: 0,
                length: fakePdfBytes.Length,
                name: "fakePdfFile",
                fileName: "fake.pdf"
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var files = new List<IFormFile> { fakePdfFile };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            var badRequestResult = result.ValidationErrors[0].ErrorMessage;
            Assert.Contains("does not match its expected format", badRequestResult);
            await fileAppService.DidNotReceive().SaveBlobAsync(Arg.Any<SaveBlobInputDto>());
        }

        [Fact]
        public async Task UploadApplicationAttachments_ContentTypeDoesNotMatchExtension_ReturnsBadRequest()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            // Valid .pdf extension and valid %PDF magic bytes, but the browser-supplied
            // ContentType claims it's an image - the content-type check should catch this
            // before the signature check even runs.
            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            var mislabeledFile = new FormFile(
                baseStream: new System.IO.MemoryStream(pdfBytes),
                baseStreamOffset: 0,
                length: pdfBytes.Length,
                name: "mislabeledFile",
                fileName: "mislabeled.pdf"
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var files = new List<IFormFile> { mislabeledFile };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            var badRequestResult = result.ValidationErrors[0].ErrorMessage;
            Assert.Contains("does not match its extension", badRequestResult);
            await fileAppService.DidNotReceive().SaveBlobAsync(Arg.Any<SaveBlobInputDto>());
        }

        [Fact]
        public async Task UploadApplicationAttachments_GenericOctetStreamContentType_UploadsSuccessfully()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            // Some clients (curl/Postman, older browsers for uncommon extensions) send the
            // generic "application/octet-stream" content type instead of a specific one - this
            // must not be treated as a mismatch as long as the extension and signature are valid.
            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            var genericFile = new FormFile(
                baseStream: new System.IO.MemoryStream(pdfBytes),
                baseStreamOffset: 0,
                length: pdfBytes.Length,
                name: "genericFile",
                fileName: "generic.pdf"
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };

            var files = new List<IFormFile> { genericFile };

            // Act
            var result = await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("All Files Are Successfully Uploaded!", okResult.Value);
            await fileAppService.Received(1).SaveBlobAsync(Arg.Is<SaveBlobInputDto>(dto => dto.Name == "generic.pdf"));
        }

        [Fact]
        public async Task UploadApplicationAttachments_ValidPdf_UploadsSuccessfully()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            var pdfFile = new FormFile(
                baseStream: new System.IO.MemoryStream(pdfBytes),
                baseStreamOffset: 0,
                length: pdfBytes.Length,
                name: "pdfFile",
                fileName: "good.pdf"
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var files = new List<IFormFile> { pdfFile };

            // Act
            var result = await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("All Files Are Successfully Uploaded!", okResult.Value);
            await fileAppService.Received(1).SaveBlobAsync(Arg.Is<SaveBlobInputDto>(dto => dto.Name == "good.pdf"));
        }

        [Fact]
        public async Task UploadApplicationAttachments_MissingAllowedFileTypesConfig_FallsBackToDefaultAllowlist()
        {
            // Arrange - deliberately built without S3:AllowedFileTypes at all, simulating an
            // environment where the config key was never set.
            var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            var pdfFile = new FormFile(
                baseStream: new System.IO.MemoryStream(pdfBytes),
                baseStreamOffset: 0,
                length: pdfBytes.Length,
                name: "pdfFile",
                fileName: "good.pdf"
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var files = new List<IFormFile> { pdfFile };

            // Act
            var result = await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert - "pdf" is on the hardcoded DefaultAllowedFileTypes list, so the upload
            // should still succeed rather than every file being rejected.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("All Files Are Successfully Uploaded!", okResult.Value);
            await fileAppService.Received(1).SaveBlobAsync(Arg.Is<SaveBlobInputDto>(dto => dto.Name == "good.pdf"));
        }

        [Fact]
        public async Task UploadApplicationAttachments_MalformedAllowedFileTypesConfig_FallsBackToDefaultAllowlist()
        {
            // Arrange - a malformed value like a real env-file quoting mistake would produce
            // (e.g. an outer-quoted JSON array, which is not valid JSON on its own) must not
            // throw an unhandled exception; it should fall back to the default allowlist.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["S3:AllowedFileTypes"] = "\"[ \"pdf\" ]\""
                })
                .Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
            var pdfFile = new FormFile(
                baseStream: new System.IO.MemoryStream(pdfBytes),
                baseStreamOffset: 0,
                length: pdfBytes.Length,
                name: "pdfFile",
                fileName: "good.pdf"
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var files = new List<IFormFile> { pdfFile };

            // Act
            var result = await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("All Files Are Successfully Uploaded!", okResult.Value);
            await fileAppService.Received(1).SaveBlobAsync(Arg.Is<SaveBlobInputDto>(dto => dto.Name == "good.pdf"));
        }

        [Fact]
        public async Task UploadApplicationAttachments_ConfigAddsDangerousExtension_StillRejectedAsInvalidFileType()
        {
            // Arrange - S3:AllowedFileTypes is misconfigured (accidentally or otherwise) to
            // include "jsp" and "exe" alongside a legitimate "pdf". Neither dangerous extension
            // is in the validated safe superset (DefaultAllowedFileTypes), so both must be
            // filtered out rather than trusted outright - otherwise they'd pass the extension
            // check and then skip content validation entirely (they're not in
            // StrictlyValidatedExtensions either), reintroducing CWE-434 via config alone.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["S3:AllowedFileTypes"] = "[\"pdf\",\"jsp\",\"exe\"]"
                })
                .Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var jspFile = new FormFile(
                baseStream: new System.IO.MemoryStream(Array.Empty<byte>()),
                baseStreamOffset: 0,
                length: 0,
                name: "jspFile",
                fileName: "shell.jsp"
            );

            var files = new List<IFormFile> { jspFile };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            var badRequestResult = result.ValidationErrors[0].ErrorMessage;
            Assert.Contains("Invalid file type", badRequestResult);
            await fileAppService.DidNotReceive().SaveBlobAsync(Arg.Any<SaveBlobInputDto>());
        }

        [Fact]
        public async Task UploadApplicationAttachments_OversizedFile_ReturnsBadRequest()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            // S3:MaxFileSize is 25 MB; this general (non-email) upload previously had no
            // server-side size enforcement at all.
            var oversizedContent = new byte[26 * 1024 * 1024];
            var oversizedFile = new FormFile(
                baseStream: new System.IO.MemoryStream(oversizedContent),
                baseStreamOffset: 0,
                length: oversizedContent.Length,
                name: "oversizedFile",
                fileName: "oversized.txt"
            );

            var files = new List<IFormFile> { oversizedFile };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            var badRequestResult = result.ValidationErrors[0].ErrorMessage;
            Assert.Contains("exceeds the maximum allowed size", badRequestResult);
            await fileAppService.DidNotReceive().SaveBlobAsync(Arg.Any<SaveBlobInputDto>());
        }

        [Fact]
        public async Task UploadEmailAttachments_ExceedsEmailPerFileMax_ReturnsBadRequest()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            emailLogAttachmentUploadService.GetTotalFileSizeByEmailLogIdAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>())
                .Returns(Task.FromResult(0L));
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var emailLogId = Guid.NewGuid();

            // S3:EmailAttachmentMaxFileSize is 20 MB - stricter than the general S3:MaxFileSize
            // of 25 MB - so a 22 MB file passes the general metadata check but must still be
            // rejected by the email-specific per-file limit, before any buffering/upload occurs.
            var oversizedContent = new byte[22 * 1024 * 1024];
            var oversizedFile = new FormFile(
                baseStream: new System.IO.MemoryStream(oversizedContent),
                baseStreamOffset: 0,
                length: oversizedContent.Length,
                name: "oversizedFile",
                fileName: "oversized.txt"
            );

            var files = new List<IFormFile> { oversizedFile };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadEmailAttachments(emailLogId, files);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            Assert.Contains("for email attachments", result.Message);
            await emailLogAttachmentUploadService.DidNotReceive().UploadAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>());
        }

        [Fact]
        public async Task UploadEmailAttachments_MalformedEmailMaxFileSizeConfig_StillEnforcesDefaultLimit()
        {
            // Arrange - S3:EmailAttachmentMaxFileSize is malformed (not a number). This must NOT
            // silently skip the per-file email size check; it must fall back to the 20 MB
            // default and still enforce it, the same way GetInvalidFileMetadata already falls
            // back for a malformed S3:MaxFileSize.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["S3:AllowedFileTypes"] = "[\"pdf\",\"txt\"]",
                    ["S3:MaxFileSize"] = "25",
                    ["S3:EmailAttachmentMaxFileSize"] = "not-a-number",
                    ["S3:EmailAttachmentsTotalMaxFileSize"] = "25"
                })
                .Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            emailLogAttachmentUploadService.GetTotalFileSizeByEmailLogIdAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>())
                .Returns(Task.FromResult(0L));
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var emailLogId = Guid.NewGuid();

            // 22 MB - under the general 25 MB cap, but over the 20 MB default email per-file cap.
            var oversizedContent = new byte[22 * 1024 * 1024];
            var oversizedFile = new FormFile(
                baseStream: new System.IO.MemoryStream(oversizedContent),
                baseStreamOffset: 0,
                length: oversizedContent.Length,
                name: "oversizedFile",
                fileName: "oversized.txt"
            );

            var files = new List<IFormFile> { oversizedFile };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadEmailAttachments(emailLogId, files);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            Assert.Contains("for email attachments", result.Message);
            await emailLogAttachmentUploadService.DidNotReceive().UploadAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>());
        }

        [Fact]
        public async Task UploadEmailAttachments_MalformedEmailTotalMaxFileSizeConfig_StillEnforcesDefaultLimit()
        {
            // Arrange - S3:EmailAttachmentsTotalMaxFileSize is malformed. Must fall back to the
            // 25 MB default and still enforce it, not silently skip the aggregate check - this is
            // the one place general uploads deliberately don't have an aggregate cap, so this
            // check being reliable matters.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["S3:AllowedFileTypes"] = "[\"pdf\",\"txt\"]",
                    ["S3:MaxFileSize"] = "25",
                    ["S3:EmailAttachmentMaxFileSize"] = "20",
                    ["S3:EmailAttachmentsTotalMaxFileSize"] = "not-a-number"
                })
                .Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            emailLogAttachmentUploadService.GetTotalFileSizeByEmailLogIdAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>())
                .Returns(Task.FromResult(0L));
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var emailLogId = Guid.NewGuid();

            // Each file is 15 MB - under the 20 MB per-file cap - but 30 MB combined exceeds the
            // 25 MB default total cap the config should have fallen back to.
            var fileContent = new byte[15 * 1024 * 1024];
            var files = new List<IFormFile>
            {
                new FormFile(
                    baseStream: new System.IO.MemoryStream(fileContent),
                    baseStreamOffset: 0,
                    length: fileContent.Length,
                    name: "file1",
                    fileName: "file1.txt"
                ),
                new FormFile(
                    baseStream: new System.IO.MemoryStream(fileContent),
                    baseStreamOffset: 0,
                    length: fileContent.Length,
                    name: "file2",
                    fileName: "file2.txt"
                )
            };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadEmailAttachments(emailLogId, files);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            Assert.Contains("would exceed the maximum allowed", result.Message);
            await emailLogAttachmentUploadService.DidNotReceive().UploadAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>());
        }

        [Fact]
        public async Task UploadEmailAttachments_ExceedsEmailTotalMax_ReturnsBadRequest()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            emailLogAttachmentUploadService.GetTotalFileSizeByEmailLogIdAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>())
                .Returns(Task.FromResult(0L));
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);
            var emailLogId = Guid.NewGuid();

            // Each file is 15 MB - under both the general S3:MaxFileSize (25 MB) and the
            // email per-file limit (20 MB) - but two of them combined (30 MB) exceed the
            // S3:EmailAttachmentsTotalMaxFileSize of 25 MB, and must be rejected before any
            // file is buffered or uploaded.
            var fileContent = new byte[15 * 1024 * 1024];
            var files = new List<IFormFile>
            {
                new FormFile(
                    baseStream: new System.IO.MemoryStream(fileContent),
                    baseStreamOffset: 0,
                    length: fileContent.Length,
                    name: "file1",
                    fileName: "file1.txt"
                ),
                new FormFile(
                    baseStream: new System.IO.MemoryStream(fileContent),
                    baseStreamOffset: 0,
                    length: fileContent.Length,
                    name: "file2",
                    fileName: "file2.txt"
                )
            };

            // Act
            async Task<IActionResult> Action() => await attachmentController.UploadEmailAttachments(emailLogId, files);

            // Assert
            var result = await Assert.ThrowsAsync<AbpValidationException>(Action);
            Assert.Contains("would exceed the maximum allowed", result.Message);
            await emailLogAttachmentUploadService.DidNotReceive().UploadAsync(
                Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<string>());
        }

        [Fact]
        public async Task DownloadChefsAttachments_ReturnsChefsAttachmentFile()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var formSubmissionId = Guid.NewGuid();
            var chefsFileAttachmentId = Guid.NewGuid();
            var fileName = "testFile.txt";
            var contentType = "application/octet-stream";
            var blobDto = new BlobDto
            {
                Name = fileName,
                Content = [],
                ContentType = contentType
            };
            submissionAppService.GetChefsFileAttachment(formSubmissionId, chefsFileAttachmentId, fileName).Returns(await Task.FromResult(blobDto));
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService);

            // Act
            Task<IActionResult> download = attachmentController.DownloadChefsAttachment(formSubmissionId, chefsFileAttachmentId, fileName);
            var downloadedFile = (FileContentResult) await download;

            // assert
            Assert.NotNull(downloadedFile);
            Assert.Equal(fileName, downloadedFile.FileDownloadName);
            Assert.Equal(contentType,downloadedFile.ContentType);
        }
    }
}
