using System.Threading.Tasks;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Scoresheets
{
    public class ScoresheetAppService : FlexAppService, IScoresheetAppService
    {
        public async Task<ScoresheetDto> CreateAsync(CreateScoresheetDto dto)
        {
            await Task.CompletedTask;
            return new ScoresheetDto() { Name = dto.Name };
        }
    }
}
