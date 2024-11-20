﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.TenantManagement;

namespace Unity.GrantManager.HealthChecks;

public class ReadyHealthCheck(ITenantRepository tenantRepository,
    ILogger<ReadyHealthCheck> logger,
    IHttpContextAccessor httpContextAccessor) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        const string readinessHeader = "readiness";

        try
        {
            var tenant = await tenantRepository.FindByNameAsync(GrantManagerConsts.NormalizedDefaultTenantName, cancellationToken: cancellationToken);
            if (tenant == null)
            {
                httpContextAccessor.HttpContext?.Response?.Headers?.Append(readinessHeader, "degraded");

                return HealthCheckResult.Degraded();
            }

            httpContextAccessor.HttpContext?.Response?.Headers?.Append(readinessHeader, "healthy");
            return HealthCheckResult.Healthy();
        }
        catch (System.Exception ex)
        {
            logger.LogError(ex, "Error during ready health check");

            httpContextAccessor.HttpContext?.Response?.Headers?.Append(readinessHeader, "unhealthy");
            return HealthCheckResult.Unhealthy();
        }
    }
}