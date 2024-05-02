using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Worksheets
{
    public interface IWorksheetAppService : IApplicationService
    {
        Task<WorksheetDto> CreateAsync(CreateWorksheetDto dto);
    }
}
