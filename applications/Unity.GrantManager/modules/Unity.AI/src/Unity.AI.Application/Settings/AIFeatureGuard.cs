using Microsoft.Extensions.Localization;
using System.Threading.Tasks;
using Unity.AI.Localization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;

namespace Unity.AI.Settings;

public class AIFeatureGuard(
    IFeatureChecker featureChecker,
    IStringLocalizer<AIResource> localizer) : ITransientDependency
{
    public async Task EnsureEnabledAsync(string featureName, string disabledMessageKey)
    {
        if (!await featureChecker.IsEnabledAsync(featureName))
        {
            throw new UserFriendlyException(localizer[disabledMessageKey]);
        }
    }
}
