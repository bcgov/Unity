using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Attachments
{
    public interface IFileAppService : IApplicationService
    {
        Task SaveBlobAsync(SaveBlobInputDto saveBlobInputDto);
        Task <BlobDto> GetBlobAsync(GetBlobRequestDto getBlobRequestDto);
        Task<bool> DeleteBlobAsync(DeleteBlobRequestDto deleteBlobRequestDto);
    }
}
