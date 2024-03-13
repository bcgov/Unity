using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Features;

namespace Unity.Payments.Samples;

[RequiresFeature("Unity.Payments")]
public class SampleAppService : PaymentsAppService, ISampleAppService
{
    public Task<SampleDto> GetAsync()
    {
        return Task.FromResult(
            new SampleDto
            {
                Value = 42
            }
        );
    }

    [Authorize]
    public Task<SampleDto> GetAuthorizedAsync()
    {
        return Task.FromResult(
            new SampleDto
            {
                Value = 42
            }
        );
    }
}
