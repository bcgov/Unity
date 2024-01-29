using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Controllers;
using Unity.GrantManager.Intakes;
using Volo.Abp.Validation;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class AttachmentControllerTests : GrantManagerWebTestBase
    {
        [Fact]
        public async Task UploadApplicationAttachments_InvalidInput_ReturnsBadRequest()
        {
            // Arrange
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", optional: false);
            var configuration = builder.Build();
            var fileAppService = Substitute.For<IFileAppService>();
            var submissionAppService = Substitute.For<ISubmissionAppService>();
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService);
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
            var blobDto = new BlobDto { Name = fileName, Content = Array.Empty<byte>(), ContentType = contentType };
            submissionAppService.GetChefsFileAttachment(formSubmissionId, chefsFileAttachmentId, fileName).Returns(await Task.FromResult(blobDto));
            var attachmentController = new AttachmentController(fileAppService, configuration, submissionAppService);

            // Act
            Task<IActionResult> download = attachmentController.DownloadChefsAttachment(formSubmissionId, chefsFileAttachmentId, fileName);
            var downloadedFile = (FileContentResult) download.Result;

            // assert
            Assert.NotNull(downloadedFile);
            Assert.Equal(fileName, downloadedFile.FileDownloadName);
            Assert.Equal(contentType,downloadedFile.ContentType);
        }
    }
}
