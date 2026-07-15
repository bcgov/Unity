using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Features;

namespace Unity.Modules.Shared.Specializations;

public class SpecializationChecker(IFeatureChecker featureChecker) : ISpecializationChecker, ITransientDependency
{
    public Task<bool> IsEnabledAsync(string name) => featureChecker.IsEnabledAsync(name);
}
