using Microsoft.AspNetCore.StaticFiles;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;

namespace Unity.GrantManager.Attachments
{
    public class FileAppService : ApplicationService, IFileAppService
    {
        private readonly IBlobContainer<S3Container> _fileContainer;

        public FileAppService(IBlobContainer<S3Container> fileContainer)
        {
            _fileContainer = fileContainer;
        }

        async Task<BlobDto> IFileAppService.GetBlobAsync(GetBlobRequestDto getBlobRequestDto)
        {
            var blob = await _fileContainer.GetAllBytesAsync(getBlobRequestDto.S3ObjectKey);
            var mimeType = GetMimeType(getBlobRequestDto.Name);
            return new BlobDto{ Name = getBlobRequestDto.Name,Content = blob, ContentType = mimeType};
        }

        async Task IFileAppService.SaveBlobAsync(SaveBlobInputDto saveBlobInputDto)
        {
            await _fileContainer.SaveAsync(saveBlobInputDto.Name, saveBlobInputDto.Content, true);
        }

        async Task<bool> IFileAppService.DeleteBlobAsync(DeleteBlobRequestDto deleteBlobRequestDto)
        {             
            return await _fileContainer.DeleteAsync(deleteBlobRequestDto.S3ObjectKey);  
        }

        private static string GetMimeType(string fileName)
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
