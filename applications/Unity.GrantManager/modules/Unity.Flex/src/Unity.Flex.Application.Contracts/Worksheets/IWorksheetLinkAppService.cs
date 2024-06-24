using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Worksheets
{
    public interface IWorksheetLinkAppService : IApplicationService
    {
        Task<WorksheetLinkDto> CreateAsync(CreateWorksheetLinkDto dto);
    }
}
