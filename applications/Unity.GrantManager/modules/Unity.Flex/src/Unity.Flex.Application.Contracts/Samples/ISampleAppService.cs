using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Samples;

public interface ISampleAppService : IApplicationService
{
    Task<SampleDto> GetAsync();

    Task<SampleDto> GetAuthorizedAsync();
}
