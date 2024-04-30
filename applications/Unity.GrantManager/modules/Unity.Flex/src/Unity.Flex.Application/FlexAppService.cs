using Unity.Flex.Localization;
using Volo.Abp.Application.Services;

namespace Unity.Flex;

public abstract class FlexAppService : ApplicationService
{
    protected FlexAppService()
    {
        LocalizationResource = typeof(FlexResource);
        ObjectMapperContext = typeof(FlexApplicationModule);
    }
}
