using Unity.GrantManager.Localization;
using Unity.GrantManager.Zones;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

namespace Unity.GrantManager;

/* Inherit your application services from this class.
 */
public abstract class GrantManagerAppService : ApplicationService
{
    protected IZoneChecker ZoneChecker => LazyServiceProvider.LazyGetRequiredService<IZoneChecker>();

    protected ILocalEventBus LocalEventBus => LazyServiceProvider.LazyGetRequiredService<ILocalEventBus>();

    protected GrantManagerAppService()
    {
        LocalizationResource = typeof(GrantManagerResource);
    }
}
