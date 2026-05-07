using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.AspNetCore.Mvc.UI.Theme.UX2.Analytics;

/// <summary>
/// Default no-op implementation. Analytics are silently disabled when the host
/// application does not register a concrete <see cref="IAnalyticsUrlProvider"/>.
/// </summary>
[Dependency(TryRegister = true)]
public class NullAnalyticsUrlProvider : IAnalyticsUrlProvider, ITransientDependency
{
    public Task<string> GetMatomoUrlAsync() => Task.FromResult(string.Empty);
}
