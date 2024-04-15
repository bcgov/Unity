using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.HealthChecks;

public class ReadyHealthCheck : IHealthCheck
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ILogger<ReadyHealthCheck> _logger;

    public ReadyHealthCheck(ITenantRepository tenantRepository, 
        ILogger<ReadyHealthCheck> logger)
    {
        _tenantRepository = tenantRepository;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName);
            if (tenant == null)
            {
                return HealthCheckResult.Degraded();
            }
            return HealthCheckResult.Healthy();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error during ready health check");
            return HealthCheckResult.Unhealthy();
        }        
    }    
}
