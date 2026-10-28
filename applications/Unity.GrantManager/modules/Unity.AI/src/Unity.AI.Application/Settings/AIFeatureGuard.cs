using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;

namespace Unity.AI.Settings;

public class AIFeatureGuard(IFeatureChecker featureChecker) : ITransientDependency
{
    public async Task EnsureEnabledAsync(string featureName, string disabledErrorCode)
    {
        if (!await featureChecker.IsEnabledAsync(featureName))
        {
            throw new BusinessException(disabledErrorCode);
        }
    }
}
