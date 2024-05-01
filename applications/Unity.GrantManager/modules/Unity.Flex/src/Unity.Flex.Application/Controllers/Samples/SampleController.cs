using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.Flex.Controllers;
using Unity.Flex.Samples;
using Volo.Abp;

namespace Unity.Flex.HttpApi;

[Area(FlexRemoteServiceConsts.ModuleName)]
[RemoteService(Name = FlexRemoteServiceConsts.RemoteServiceName)]
[Route("api/Flex/sample")]
public class SampleController : FlexController, ISampleAppService
{
    private readonly ISampleAppService _sampleAppService;

    public SampleController(ISampleAppService sampleAppService)
    {
        _sampleAppService = sampleAppService;
    }

    [HttpGet]
    public async Task<SampleDto> GetAsync()
    {
        return await _sampleAppService.GetAsync();
    }

    [HttpGet]
    [Route("authorized")]
    [Authorize]
    public async Task<SampleDto> GetAuthorizedAsync()
    {
        return await _sampleAppService.GetAsync();
    }
}
