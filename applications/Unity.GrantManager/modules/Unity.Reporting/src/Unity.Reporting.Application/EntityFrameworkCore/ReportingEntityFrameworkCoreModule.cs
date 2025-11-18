using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Unity.Reporting.EntityFrameworkCore;

/// <summary>
/// ABP Framework module for configuring Entity Framework Core integration with the Unity.Reporting module.
/// Registers the ReportingDbContext with the ABP DbContext management system and configures
/// any custom repository implementations needed for advanced database operations.
/// </summary>
public class ReportingEntityFrameworkCoreModule : AbpModule
{
    /// <summary>
    /// Configures Entity Framework Core services for the Unity.Reporting module.
    /// Registers the ReportingDbContext with ABP's database context management
    /// and sets up dependency injection for custom repositories.
    /// </summary>
    /// <param name="context">The service configuration context for dependency injection setup.</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<ReportingDbContext>(options =>
        {
            /* Add custom repositories here */
        });
    }
}
