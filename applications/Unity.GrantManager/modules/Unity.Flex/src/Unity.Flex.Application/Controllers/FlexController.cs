using Unity.Flex.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.HttpApi;

public abstract class FlexController : AbpControllerBase
{
    protected FlexController()
    {
        LocalizationResource = typeof(FlexResource);
    }
}
