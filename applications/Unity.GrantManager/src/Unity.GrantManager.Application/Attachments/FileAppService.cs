using System;
using System.Threading.Tasks;
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

        Task<BlobDto> IFileAppService.GetBlobAsync(GetBlobRequestDto getBlobRequestDto)
        {
            throw new NotImplementedException();
        }

        async Task IFileAppService.SaveBlobAsync(SaveBlobInputDto saveBlobInputDto)
        {
            await _fileContainer.SaveAsync(saveBlobInputDto.Name, saveBlobInputDto.Content, true);
        }
    }
}
