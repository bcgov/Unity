using Volo.Abp.Application.Services;

namespace Unity.AI;

public abstract class AIAppService : ApplicationService
{
    protected AIAppService()
    {
        LocalizationResource = typeof(Localization.AIResource);
    }
}
