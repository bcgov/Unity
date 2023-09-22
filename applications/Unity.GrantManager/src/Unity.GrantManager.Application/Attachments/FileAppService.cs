using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        Task<BlobDto> IFileAppService.GetBlobAsync(GetBlobRequestDto input)
        {
            throw new NotImplementedException();
        }

        async Task IFileAppService.SaveBlobAsync(SaveBlobInputDto input)
        {
            await _fileContainer.SaveAsync(input.Name, input.Content, true);
        }
    }
}
