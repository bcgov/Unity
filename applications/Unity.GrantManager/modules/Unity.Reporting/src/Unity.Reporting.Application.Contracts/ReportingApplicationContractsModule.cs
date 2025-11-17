using Volo.Abp.Application;
using Volo.Abp.Modularity;
using Volo.Abp.Authorization;

namespace Unity.Reporting;

/// <summary>
/// ABP Framework module for Unity.Reporting Application Contracts layer defining service interfaces, DTOs, and permissions.
/// Configures dependencies for domain shared module, application contracts base functionality, and authorization features.
/// Provides the contract definitions for all Unity Reporting application services without implementation details.
/// </summary>
[DependsOn(
    typeof(ReportingDomainSharedModule),
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpAuthorizationModule)
    )]
public class ReportingApplicationContractsModule : AbpModule
{

}
