using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Features;
using Volo.Abp.SimpleStateChecking;

namespace Unity.AI.Permissions;

public class AnyFeaturePermissionStateProvider(params string[] features) : ISimpleStateChecker<PermissionDefinition>
{
    private readonly string[] _features = features;

    public async Task<bool> IsEnabledAsync(SimpleStateCheckerContext<PermissionDefinition> context)
    {
        var featureChecker = context.ServiceProvider.GetRequiredService<IFeatureChecker>();

        foreach (var feature in _features)
        {
            if (await featureChecker.IsEnabledAsync(feature))
            {
                return true;
            }
        }

        return false;
    }
}
