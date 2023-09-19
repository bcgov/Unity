<<<<<<< HEAD
﻿using System;
=======
﻿using Microsoft.AspNetCore.StaticFiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
>>>>>>> 8c2966d (Download attachments)
using System.Threading.Tasks;
using System.Xml.Linq;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;

namespace Unity.GrantManager.Attachments
{
    public class FileAppService : ApplicationService, IFileAppService
    {
        private readonly IBlobContainer<ComsS3Container> _fileContainer;

        public FileAppService(IBlobContainer<ComsS3Container> fileContainer)
        {
            _fileContainer = fileContainer;
        }

        async Task<BlobDto> IFileAppService.GetBlobAsync(GetBlobRequestDto getBlobRequestDto)
        {
            var blob = await _fileContainer.GetAllBytesAsync(getBlobRequestDto.S3Guid.ToString());
            var mimeType = GetMimeType(getBlobRequestDto.Name);
            return new BlobDto{ Name = getBlobRequestDto.Name,Content = blob, ContentType = mimeType};
        }

        async Task IFileAppService.SaveBlobAsync(SaveBlobInputDto saveBlobInputDto)
        {
            await _fileContainer.SaveAsync(saveBlobInputDto.Name, saveBlobInputDto.Content, true);
        }

        private string GetMimeType(string fileName)
        {
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(fileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }
            return contentType;
        }

    }
}
