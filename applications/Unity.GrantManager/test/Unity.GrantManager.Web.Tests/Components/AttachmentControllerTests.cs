using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Controllers;
using Xunit;

namespace Unity.GrantManager.Components
{
    public class AttachmentControllerTests : GrantManagerWebTestBase
    {
        [Fact]
        public async Task UploadApplicationAttachments_InvalidInput_ReturnsBadRequest()
        {
            // Arrange
            var myConfiguration = new Dictionary<string, string>
            {
                {"S3:DisallowedFileTypes", "[\"exe\"]"},
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
            var fileAppService = Substitute.For<IFileAppService>();            
            var attachmentController = new AttachmentController(fileAppService, configuration);
            var applicationId = Guid.NewGuid();
            var userId = "testUserId";
            var userName = "testUserName";

            var invalidFile = new FormFile(
                baseStream: new System.IO.MemoryStream(new byte[0]),
                baseStreamOffset: 0,
                length: 0,
                name: "invalidFile",
                fileName: "invalidFile.exe"
            );

            var files = new List<IFormFile> { invalidFile };

            // Act
            var response = await attachmentController.UploadApplicationAttachments(applicationId, files, userId, userName);

            // Assert
            Assert.IsType<BadRequestObjectResult>(response);
            var badRequestResult = (BadRequestObjectResult)response;
            Assert.Contains("ERROR: Following has invalid file types", badRequestResult?.Value?.ToString());
        }
    }
}
