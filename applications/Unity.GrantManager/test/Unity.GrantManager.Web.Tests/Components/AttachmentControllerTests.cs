using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            // Valid .pdf extension, but the browser-supplied ContentType claims it's an image -
            // the content-type check should catch this mismatch.
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            // Some clients (curl/Postman, older browsers for uncommon extensions) send the
            // generic "application/octet-stream" content type instead of a specific one - this
            // must not be treated as a mismatch as long as the extension is valid.
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
        public async Task UploadApplicationAttachments_EmlFile_UploadsSuccessfully()
        {
            // Arrange - .eml is a plain-text (RFC 822) saved-email format, added to the allowlist
            // so users can attach a saved email as evidence/correspondence. Its ContentType
            // reporting is inconsistent across mail clients/OSes, so it's exempt from the
            // content-type consistency check (same treatment as txt/csv), and only the
            // allowlist and size checks apply.
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var emlBytes = System.Text.Encoding.UTF8.GetBytes("From: a@example.com\r\nTo: b@example.com\r\nSubject: Test\r\n\r\nBody");
            var emlFile = new FormFile(
                baseStream: new System.IO.MemoryStream(emlBytes),
                baseStreamOffset: 0,
                length: emlBytes.Length,
                name: "emlFile",
                fileName: "saved-email.eml"
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "message/rfc822"
            };

            var files = new List<IFormFile> { emlFile };

            // Act
            var result = await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("All Files Are Successfully Uploaded!", okResult.Value);
            await fileAppService.Received(1).SaveBlobAsync(Arg.Is<SaveBlobInputDto>(dto => dto.Name == "saved-email.eml"));
        }

        [Fact]
        public async Task UploadApplicationAttachments_OutlookMsgFile_UploadsSuccessfully()
        {
            // Arrange - .msg is Outlook's native saved-email format (an OLE compound file under
            // the hood, same container family as legacy .doc/.xls/.ppt), added alongside .eml so
            // Outlook users can save and attach an email without converting it first.
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var msgFile = new FormFile(
                baseStream: new System.IO.MemoryStream(Array.Empty<byte>()),
                baseStreamOffset: 0,
                length: 0,
                name: "msgFile",
                fileName: "saved-email.msg"
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };

            var files = new List<IFormFile> { msgFile };

            // Act
            var result = await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("All Files Are Successfully Uploaded!", okResult.Value);
            await fileAppService.Received(1).SaveBlobAsync(Arg.Is<SaveBlobInputDto>(dto => dto.Name == "saved-email.msg"));
        }

        [Fact]
        public async Task UploadApplicationAttachments_OpenDocumentTextFile_UploadsSuccessfully()
        {
            // Arrange - .odt (LibreOffice/OpenOffice Writer) added alongside the OOXML formats so
            // users of either office suite can upload native documents.
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var odtFile = new FormFile(
                baseStream: new System.IO.MemoryStream(Array.Empty<byte>()),
                baseStreamOffset: 0,
                length: 0,
                name: "odtFile",
                fileName: "document.odt"
            )
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/vnd.oasis.opendocument.text"
            };

            var files = new List<IFormFile> { odtFile };

            // Act
            var result = await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("All Files Are Successfully Uploaded!", okResult.Value);
            await fileAppService.Received(1).SaveBlobAsync(Arg.Is<SaveBlobInputDto>(dto => dto.Name == "document.odt"));
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
        public async Task UploadApplicationAttachments_AllowedFileTypesConfigContainsNullElement_FallsBackGracefully()
        {
            // Arrange - ["pdf", null] is syntactically valid JSON (e.g. from a stray trailing
            // comma edit) and deserializes fine to a string[] containing a null entry. Filtering
            // must happen before ToLowerInvariant() is called on each entry, or this throws a
            // NullReferenceException and turns a config typo into a 500 on every upload instead of
            // falling back gracefully like every other malformed-config case.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["S3:AllowedFileTypes"] = "[\"pdf\", null]"
                })
                .Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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

            // Assert - "pdf" (the non-null entry) is honored, upload succeeds instead of 500ing.
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("All Files Are Successfully Uploaded!", okResult.Value);
            await fileAppService.Received(1).SaveBlobAsync(Arg.Is<SaveBlobInputDto>(dto => dto.Name == "good.pdf"));
        }

        [Fact]
        public async Task UploadApplicationAttachments_ConfigAddsExtensionOutsideDefaultSet_IsAccepted()
        {
            // Arrange - S3:AllowedFileTypes deliberately includes "jsp", which is outside
            // DefaultAllowedFileTypes. Config is an operational trust boundary (set by whoever
            // controls the deployment environment, not a remote caller), so once present it is
            // the effective allowlist outright - DefaultAllowedFileTypes is only a fallback for
            // when config is missing/malformed, not a ceiling on what config can specify.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["S3:AllowedFileTypes"] = "[\"pdf\",\"jsp\"]"
                })
                .Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
            var result = await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("All Files Are Successfully Uploaded!", okResult.Value);
            await fileAppService.Received(1).SaveBlobAsync(Arg.Is<SaveBlobInputDto>(dto => dto.Name == "shell.jsp"));
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());
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
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());

            // Act
            Task<IActionResult> download = attachmentController.DownloadChefsAttachment(formSubmissionId, chefsFileAttachmentId, fileName);
            var downloadedFile = (FileContentResult) await download;

            // assert
            Assert.NotNull(downloadedFile);
            Assert.Equal(fileName, downloadedFile.FileDownloadName);
            Assert.Equal(contentType,downloadedFile.ContentType);
        }

        // Security regression tests (CWE-639 / IDOR): the controller must confirm the id in the
        // route belongs to a real, tenant-scoped entity before ever touching S3. FindAsync returning
        // null stands in for both "no such id" and "id belongs to another tenant" - ABP's automatic
        // IMultiTenant query filter makes those indistinguishable at the repository level, which is
        // exactly the property this fix relies on.

        [Fact]
        public async Task DownloadApplicantAttachment_ApplicantNotFoundOrWrongTenant_ReturnsNotFound()
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
            var applicantRepository = Substitute.For<IApplicantRepository>();
            applicantRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns((Applicant?)null);
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, applicantRepository, Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());

            var fileName = "secret.pdf";
            fileAppService.GetBlobAsync(Arg.Any<GetBlobRequestDto>())
                .Returns(new BlobDto { Name = fileName, Content = [1, 2, 3], ContentType = "application/pdf" });

            // Act
            var result = await attachmentController.DownloadApplicantAttachment(Guid.NewGuid().ToString(), fileName);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            await fileAppService.DidNotReceive().GetBlobAsync(Arg.Any<GetBlobRequestDto>());
        }

        [Fact]
        public async Task DownloadApplicationAttachment_ApplicationNotFoundOrWrongTenant_ReturnsNotFound()
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
            var applicationRepository = Substitute.For<IApplicationRepository>();
            applicationRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns((Application?)null);
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), applicationRepository, Substitute.For<IAssessmentRepository>());

            var fileName = "secret.pdf";
            fileAppService.GetBlobAsync(Arg.Any<GetBlobRequestDto>())
                .Returns(new BlobDto { Name = fileName, Content = [1, 2, 3], ContentType = "application/pdf" });

            // Act
            var result = await attachmentController.DownloadApplicationAttachment(Guid.NewGuid().ToString(), fileName);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            await fileAppService.DidNotReceive().GetBlobAsync(Arg.Any<GetBlobRequestDto>());
        }

        [Fact]
        public async Task DownloadAssessmentAttachment_AssessmentNotFoundOrWrongTenant_ReturnsNotFound()
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
            var assessmentRepository = Substitute.For<IAssessmentRepository>();
            assessmentRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns((Assessment?)null);
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, Substitute.For<IApplicantRepository>(), Substitute.For<IApplicationRepository>(), assessmentRepository);

            var fileName = "secret.pdf";
            fileAppService.GetBlobAsync(Arg.Any<GetBlobRequestDto>())
                .Returns(new BlobDto { Name = fileName, Content = [1, 2, 3], ContentType = "application/pdf" });

            // Act
            var result = await attachmentController.DownloadAssessmentAttachment(Guid.NewGuid().ToString(), fileName);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            await fileAppService.DidNotReceive().GetBlobAsync(Arg.Any<GetBlobRequestDto>());
        }

        [Fact]
        public async Task PreviewApplicantAttachment_ApplicantNotFoundOrWrongTenant_ReturnsNotFound()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var emailLogAttachmentUploadService = Substitute.For<IEmailLogAttachmentUploadService>();
            var currentTenant = Substitute.For<ICurrentTenant>();
            var libreOfficeConversionService = Substitute.For<ILibreOfficeConversionService>();
            libreOfficeConversionService.IsInstalled().Returns(true);
            var attachmentPreviewAppService = Substitute.For<IAttachmentPreviewAppService>();
            var applicantRepository = Substitute.For<IApplicantRepository>();
            applicantRepository.FindAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<System.Threading.CancellationToken>())
                .Returns((Applicant?)null);
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService, emailLogAttachmentUploadService, currentTenant, libreOfficeConversionService, attachmentPreviewAppService, applicantRepository, Substitute.For<IApplicationRepository>(), Substitute.For<IAssessmentRepository>());

            var fileName = "secret.pdf";
            attachmentPreviewAppService.GetOrCreatePreviewPdfAsync(AttachmentType.APPLICANT, Arg.Any<Guid>(), fileName)
                .Returns(new BlobDto { Name = fileName, Content = [1, 2, 3], ContentType = "application/pdf" });

            // Act
            var result = await attachmentController.PreviewApplicantAttachment(Guid.NewGuid().ToString(), fileName);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            await attachmentPreviewAppService.DidNotReceive().GetOrCreatePreviewPdfAsync(Arg.Any<AttachmentType>(), Arg.Any<Guid>(), Arg.Any<string>());
        }
    }
}
