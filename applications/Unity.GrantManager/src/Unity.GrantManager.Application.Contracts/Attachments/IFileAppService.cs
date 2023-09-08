using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Attachments
{
    public interface IFileAppService : IApplicationService
    {
        Task SaveBlobAsync(SaveBlobInputDto saveBlobInputDto);
        Task <BlobDto> GetBlobAsync(GetBlobRequestDto getBlobRequestDto);
    }
}
