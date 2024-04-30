using System.Threading.Tasks;

namespace Unity.Flex.Worksheets
{
    public class WorksheetAppService : FlexAppService, IWorksheetAppService
    {
        public async Task<WorksheetDto> CreateAsync(CreateWorksheetDto dto)
        {
            await Task.CompletedTask;
            return new WorksheetDto() { Name = dto.Name };
        }
    }
}
